using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Rolling-buffer microphone recorder used for the death-sequence playback
/// (§4.5) and the Playback Trap mimicry system (§3.5 / §4.4).
/// </summary>
public partial class VoiceRecorder : Node
{
	[Export] public string BusName = "Microphone";
	[Export] public float BufferSeconds = 10.0f;
	[Export] public float SegmentSeconds = 2.0f;
	[Export] public bool AutoStart = true;

	private AudioEffectRecord _recorder;
	private int _recorderIndex = -1;
	private int _busIndex = -1;
	private bool _recording = false;
	private double _segmentTimer = 0.0;

	private readonly Queue<AudioStreamWav> _segments = new();
	private float _currentSegmentMaxSeconds = 0f;

	public bool IsReady => _recorder != null && _busIndex >= 0;

	public override void _Ready()
	{
		SetupRecorder();
		if (AutoStart && IsReady)
			StartRecording();
	}

	public override void _Process(double delta)
	{
		if (!_recording || _recorder == null)
			return;

		_segmentTimer += delta;
		if (_segmentTimer >= SegmentSeconds)
		{
			_segmentTimer = 0.0;
			CycleSegment();
		}
	}

	public void SetupRecorder()
	{
		if (_recorder != null)
			return;

		_busIndex = AudioServer.GetBusIndex(BusName);
		if (_busIndex < 0)
		{
			GD.PushWarning($"VoiceRecorder: audio bus '{BusName}' not found. Recording unavailable.");
			return;
		}

		_recorder = new AudioEffectRecord();
		_recorderIndex = AudioServer.GetBusEffectCount(_busIndex);
		AudioServer.AddBusEffect(_busIndex, _recorder);
		GD.Print($"VoiceRecorder: attached AudioEffectRecord to bus '{BusName}' at effect index {_recorderIndex}.");
	}

	public void StartRecording()
	{
		if (_recorder == null || _recording)
			return;

		if (AudioServer.GetInputDeviceList().Length == 0)
		{
			GD.PushWarning("VoiceRecorder: no input device available. Rolling capture disabled.");
			return;
		}

		_recorder.SetRecordingActive(true);
		_recording = true;
		_segmentTimer = 0.0;
		GD.Print("VoiceRecorder: started rolling capture.");
	}

	public void StopRecording()
	{
		if (_recorder == null || !_recording)
			return;

		_recorder.SetRecordingActive(false);
		_recording = false;
	}

	private void CycleSegment()
	{
		if (_recorder == null)
			return;

		float leftPeak = AudioServer.GetBusPeakVolumeLeftDb(_busIndex, 0);
		float rightPeak = AudioServer.GetBusPeakVolumeRightDb(_busIndex, 0);
		if (leftPeak <= -79.0f && rightPeak <= -79.0f)
			return;

		_recorder.SetRecordingActive(false);
		AudioStreamWav clip = _recorder.GetRecording();
		_recorder.SetRecordingActive(true);

		if (clip == null)
			return;

		float clipLen = (float)clip.GetLength();
		if (clipLen > 0.01f)
		{
			_segments.Enqueue(clip);
			_currentSegmentMaxSeconds += clipLen;
		}

		while (_currentSegmentMaxSeconds > BufferSeconds && _segments.Count > 0)
		{
			AudioStreamWav old = _segments.Dequeue();
			if (old != null)
				_currentSegmentMaxSeconds -= (float)old.GetLength();
		}
	}

	/// <summary>
	/// Returns the most recent continuous recording up to <paramref name="maxSeconds"/>.
	/// </summary>
	public AudioStreamWav GetRecentRecording(float maxSeconds = 10f)
	{
		if (_segments.Count == 0)
			return null;

		List<AudioStreamWav> clips = new();
		float total = 0f;
		AudioStreamWav[] array = _segments.ToArray();
		for (int i = array.Length - 1; i >= 0; i--)
		{
			AudioStreamWav clip = array[i];
			if (clip == null)
				continue;
			float len = (float)clip.GetLength();
			if (total + len > maxSeconds && clips.Count > 0)
				break;
			clips.Insert(0, clip);
			total += len;
		}

		if (clips.Count == 0)
			return null;
		if (clips.Count == 1)
			return clips[0];

		return MergeClips(clips);
	}

	private AudioStreamWav MergeClips(List<AudioStreamWav> clips)
	{
		if (clips.Count == 0)
			return null;

		int sampleRate = clips[0].MixRate;
		int channels = clips[0].Stereo ? 2 : 1;
		int format = (int)clips[0].Format;
		int bytesPerSample = format == (int)AudioStreamWav.FormatEnum.ImaAdpcm ? 1 : (format == (int)AudioStreamWav.FormatEnum.Format16Bits ? 2 : 1);
		if (channels == 2)
			bytesPerSample *= 2;

		using (var stream = new System.IO.MemoryStream())
		{
			foreach (AudioStreamWav clip in clips)
			{
				byte[] data = clip.Data;
				if (data != null)
					stream.Write(data, 0, data.Length);
			}

			AudioStreamWav merged = new();
			merged.Format = (AudioStreamWav.FormatEnum)format;
			merged.MixRate = sampleRate;
			merged.Stereo = channels == 2;
			merged.Data = stream.ToArray();
			return merged;
		}
	}

	public override void _ExitTree()
	{
		StopRecording();
		if (_busIndex >= 0 && _recorder != null)
		{
			for (int i = AudioServer.GetBusEffectCount(_busIndex) - 1; i >= 0; i--)
			{
				if (ReferenceEquals(AudioServer.GetBusEffect(_busIndex, i), _recorder))
				{
					AudioServer.RemoveBusEffect(_busIndex, i);
					break;
				}
			}
		}
	}
}
