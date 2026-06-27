#!/usr/bin/env python3
"""
Procedural audio generator for Last Word.

Generates placeholder WAV files for audio items still missing from §12 Audio
Design that don't yet have an asset. All output is 16-bit PCM, mono, 44.1 kHz
to match the existing `Assets/Audio/**` convention.

These files are intentionally minimal (synthesised, not recorded) so the
audio slots in §12 can be exercised in playtests before a real audio pass.
Replace with licensed/recorded SFX when available — file names are stable so
the .import files and AudioAssets.cs paths stay valid.

Outputs:
    Assets/Audio/listener/listener_hunting_breath.wav
    Assets/Audio/listener/listener_frenzy_tone.wav
    Assets/Audio/listener/listener_catch_silence.wav
    Assets/Audio/world/gramophone_music_loop.wav
"""

from __future__ import annotations

import math
import os
import random
import struct
import wave

SR = 44100  # Sample rate, matches existing WAVs in repo

# Project root is two levels up from this script: .kimchi/tools/ -> project root
PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ASSETS = os.path.join(PROJECT_ROOT, "Assets", "Audio")


# ---------------------------------------------------------------------------
# Low-level WAV writer
# ---------------------------------------------------------------------------

def write_wav(path: str, samples: list[float]) -> None:
    """Write a list of floats in [-1, 1] as 16-bit mono PCM @ 44.1 kHz."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    # Clip to int16 range
    clipped = [max(-1.0, min(1.0, s)) for s in samples]
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)  # 16-bit
        w.setframerate(SR)
        # Pack as little-endian signed 16-bit
        frames = b"".join(struct.pack("<h", int(s * 32767)) for s in clipped)
        w.writeframes(frames)
    print(f"  wrote {path} ({len(samples) / SR:.2f}s, {os.path.getsize(path)} bytes)")


def normalize(samples: list[float], peak: float = 0.9) -> list[float]:
    """Peak-normalise to `peak` (avoids clipping on summation)."""
    m = max(abs(s) for s in samples) or 1.0
    return [s * (peak / m) for s in samples]


def crossfade_loop(samples: list[float], fade_seconds: float = 0.25) -> list[float]:
    """Apply equal-power crossfade between tail and head for seamless loop."""
    n = int(fade_seconds * SR)
    if n * 2 >= len(samples):
        return samples
    head = samples[:n]
    tail = samples[-n:]
    mixed = [
        tail[i] * math.cos(i / n * math.pi / 2) + head[i] * math.sin(i / n * math.pi / 2)
        for i in range(n)
    ]
    return samples[:-n] + mixed + samples[n:]


# ---------------------------------------------------------------------------
# Noise generators
# ---------------------------------------------------------------------------

def white_noise(rng: random.Random) -> float:
    return rng.uniform(-1.0, 1.0)


def pink_noise(n: int, rng: random.Random) -> list[float]:
    """Voss-McCartney pink noise approximation."""
    rows = 16
    buf = [0.0] * rows
    out = []
    for _ in range(n):
        idx = rng.randrange(rows)
        buf[idx] = rng.uniform(-1.0, 1.0)
        out.append(sum(buf) / rows * 0.6)
    return out


# ---------------------------------------------------------------------------
# §12.2 Listener Hunting breath (3 s, loopable, 8 m audible)
# ---------------------------------------------------------------------------

def generate_hunting_breath() -> list[float]:
    """
    Low rumbling breath: pink-noise breath envelope at ~0.4 Hz, layered with a
    low-frequency rumble around 60 Hz and a slightly higher 110 Hz overtone.
    Designed to sound oppressive close-up but roll off into a hum at 8 m.
    """
    duration = 3.0
    rng = random.Random(0xBEEF)
    n = int(duration * SR)

    noise = pink_noise(n, rng)

    out = [0.0] * n
    for i in range(n):
        t = i / SR
        # Breath envelope: two short inhales per loop (inhale ~0.6 s, exhale ~0.9 s)
        phase = (t % 1.5) / 1.5
        if phase < 0.4:
            env = phase / 0.4  # inhale ramp
        elif phase < 0.5:
            env = 1.0 - (phase - 0.4) * 5  # quick dip
        elif phase < 0.95:
            env = 0.15 + (phase - 0.5) * 1.5  # slow exhale ramp
        else:
            env = 1.0 - (phase - 0.95) * 20  # cut off

        env = max(0.0, min(1.0, env))

        # Low-frequency body
        body = (
            math.sin(2 * math.pi * 60 * t) * 0.35
            + math.sin(2 * math.pi * 110 * t + 1.7) * 0.18
            + math.sin(2 * math.pi * 80 * t + 0.4) * 0.22
        )

        sample = noise[i] * 0.5 * env + body * 0.25 * env
        out[i] = sample

    out = normalize(out, peak=0.85)
    return crossfade_loop(out, fade_seconds=0.2)


# ---------------------------------------------------------------------------
# §12.2 Listener Frenzy tone (2 s, loopable)
# ---------------------------------------------------------------------------

def generate_frenzy_tone() -> list[float]:
    """
    High, dissonant, slightly detuned cluster with harsh noise bursts. Should
    feel frantic — three high partials (1200, 1450, 1820 Hz) with micro-detune
    plus a sharp metallic ring every 0.4 s.
    """
    duration = 2.0
    rng = random.Random(0xCAFE)
    n = int(duration * SR)

    out = [0.0] * n
    for i in range(n):
        t = i / SR
        # Detuned cluster
        cluster = (
            math.sin(2 * math.pi * 1200 * t) * 0.30
            + math.sin(2 * math.pi * 1450 * t + 0.7) * 0.28
            + math.sin(2 * math.pi * 1820 * t + 1.3) * 0.22
            + math.sin(2 * math.pi * 2400 * t + 2.1) * 0.10
        )
        # LFO that "wobbles" the cluster amplitude
        wobble = 0.6 + 0.4 * math.sin(2 * math.pi * 7.0 * t)
        # Sharp metallic hit every 0.4 s with quick decay
        hit_phase = t % 0.4
        if hit_phase < 0.05:
            hit_env = math.exp(-hit_phase * 80.0)
            hit = (math.sin(2 * math.pi * 3200 * t) * 0.6
                   + rng.uniform(-1.0, 1.0) * 0.4) * hit_env
        else:
            hit = 0.0
        # Noise wash
        noise = rng.uniform(-1.0, 1.0) * 0.10

        out[i] = cluster * wobble * 0.55 + hit + noise

    out = normalize(out, peak=0.85)
    return crossfade_loop(out, fade_seconds=0.15)


# ---------------------------------------------------------------------------
# §12.2 Catch silence (1 s, one-shot)
# ---------------------------------------------------------------------------

def generate_catch_silence() -> list[float]:
    """
    Three-beat impact: low thud, brief vacuum (silence), muffled drop. The
    'silence' beat is what makes it feel like the air is being stolen.
    """
    duration = 1.0
    n = int(duration * SR)
    rng = random.Random(0xDEAD)

    out = [0.0] * n
    for i in range(n):
        t = i / SR
        if t < 0.08:
            # Impact thud: low body + click
            env = math.exp(-t * 35.0)
            sample = (
                math.sin(2 * math.pi * 55 * t) * 0.7
                + math.sin(2 * math.pi * 110 * t + 0.3) * 0.4
                + rng.uniform(-1.0, 1.0) * 0.3
            ) * env
        elif t < 0.45:
            # Vacuum beat — decaying noise bed, then true silence window
            env = math.exp(-(t - 0.08) * 6.0) * 0.5
            sample = rng.uniform(-1.0, 1.0) * env * 0.15
            # Drop to near-zero by 0.3 s to read as "silence"
            if t > 0.28:
                sample *= max(0.0, 1.0 - (t - 0.28) * 8.0)
        else:
            # Muffled reverb tail: pink noise filtered with low-frequency body
            tail_env = math.exp(-(t - 0.45) * 3.5)
            sample = (
                math.sin(2 * math.pi * 75 * t + 1.1) * 0.3
                + rng.uniform(-1.0, 1.0) * 0.12
            ) * tail_env
        out[i] = sample

    return normalize(out, peak=0.9)


# ---------------------------------------------------------------------------
# §12.3 Gramophone crackled music (10 s, loopable)
# ---------------------------------------------------------------------------

def generate_gramophone_loop() -> list[float]:
    """
    Slow waltz-like tune in C major, rendered as if played on a degraded
    vinyl record: band-limited, with continuous crackle + a few louder pops.
    Tempo ~80 BPM; 10 s = ~13 beats so the loop feels musical.
    """
    duration = 10.0
    rng = random.Random(0xFADE)
    n = int(duration * SR)

    # Note frequencies (C major, octave 4–5)
    note_hz = {
        "C4": 261.63, "D4": 293.66, "E4": 329.63, "F4": 349.23,
        "G4": 392.00, "A4": 440.00, "B4": 493.88, "C5": 523.25,
        "rest": 0.0,
    }

    # Simple looped melody (each tuple = note, duration in beats)
    # 13 beats at 80 BPM = 9.75 s, plus 0.25 s tail.
    beat = 60.0 / 80.0
    melody = [
        ("C4", 1), ("E4", 1), ("G4", 1), ("C5", 1),
        ("B4", 1), ("G4", 1), ("E4", 1), ("C4", 1),
        ("F4", 1), ("A4", 1), ("C5", 1), ("F4", 1),
        ("rest", 1),
    ]
    # Bass line on the tonic, alternating octave for oom-pah feel.
    # Tuple format: (note_name, frequency_hz). Note names are unused at runtime
    # but kept for readability.
    bass = [
        ("C3", 130.81), ("G3", 196.00), ("A3", 220.00), ("F3", 174.61),
    ]

    # Pre-compute per-sample (start_t, end_t, frequency) across the timeline.
    melody_seq = []
    bass_seq = []
    t_cursor = 0.0
    for note_name, beats in melody:
        hz = note_hz[note_name]
        end_t = t_cursor + beats * beat
        melody_seq.append((t_cursor, end_t, hz))
        t_cursor = end_t
    bass_cursor = 0.0
    for _name, bass_freq in bass:
        end_t = bass_cursor + beat * 3.25  # one bass note per bar-ish
        bass_seq.append((bass_cursor, end_t, bass_freq))
        bass_cursor = end_t

    out = [0.0] * n
    for i in range(n):
        t = i / SR

        # Melody: pluck-ish envelope, fundamental + 1 harmonic, vibrato
        mel_hz = 0.0
        for (t0, t1, hz) in melody_seq:
            if t0 <= t < t1:
                mel_hz = hz
                break
        bass_hz = 0.0
        for (t0, t1, hz) in bass_seq:
            if t0 <= t < t1:
                bass_hz = hz
                break

        # Note envelope: short attack, sustain, release at end of note
        sample = 0.0
        if mel_hz > 0:
            phase = (t - next(t0 for (t0, _, _) in melody_seq if t0 <= t)) % 1.0
            note_pos = t - next(t0 for (t0, _, _) in melody_seq if t0 <= t)
            note_dur = next(t1 - t0 for (t0, t1, _) in melody_seq if t0 <= t)
            if note_pos < 0.02:
                env = note_pos / 0.02
            elif note_pos > note_dur - 0.05:
                env = max(0.0, (note_dur - note_pos) / 0.05)
            else:
                env = 1.0
            vibrato = 1.0 + 0.005 * math.sin(2 * math.pi * 5.0 * t)
            sample += (
                math.sin(2 * math.pi * mel_hz * vibrato * t) * 0.30
                + math.sin(2 * math.pi * mel_hz * 2 * t) * 0.10
            ) * env

        if bass_hz > 0:
            note_pos = t - next(t0 for (t0, _, _) in bass_seq if t0 <= t)
            note_dur = next(t1 - t0 for (t0, t1, _) in bass_seq if t0 <= t)
            if note_pos < 0.01:
                env = note_pos / 0.01
            elif note_pos > note_dur - 0.05:
                env = max(0.0, (note_dur - note_pos) / 0.05)
            else:
                env = 0.7
            sample += math.sin(2 * math.pi * bass_hz * t) * 0.18 * env

        # Continuous crackle: high-pass-ish pink noise with envelope
        crackle = rng.uniform(-1.0, 1.0) * 0.07
        sample += crackle

        # Loud pops (sparse)
        if rng.random() < 0.0008:
            sample += rng.uniform(-1.0, 1.0) * 0.4

        # Vinyl low-pass: average with previous sample for dullness
        if i > 0:
            sample = sample * 0.55 + out[i - 1] * 0.45

        out[i] = sample

    out = normalize(out, peak=0.85)
    return crossfade_loop(out, fade_seconds=0.3)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    targets = [
        ("listener/listener_hunting_breath.wav", generate_hunting_breath),
        ("listener/listener_frenzy_tone.wav", generate_frenzy_tone),
        ("listener/listener_catch_silence.wav", generate_catch_silence),
        ("world/gramophone_music_loop.wav", generate_gramophone_loop),
    ]

    print(f"Writing {len(targets)} WAV file(s) to {ASSETS}")
    for rel, gen in targets:
        path = os.path.join(ASSETS, rel)
        write_wav(path, gen())

    print("Done.")


if __name__ == "__main__":
    main()
