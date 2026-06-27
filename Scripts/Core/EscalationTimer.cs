using Godot;
using System;

namespace LastWord.Core
{
	public partial class EscalationTimer : Node
	{
		private bool _escalationFired = false;
		private bool _duplicationFired = false;
		private bool _lightsOutFired = false;
		private bool _finalCountdownFired = false;
		private bool _finalDeadlineFired = false;

		private float _escalationTime = 600f; // 10 min
		private float _duplicationTime = 1200f; // 20 min
		private float _lightsOutTime = 1500f; // 25 min
		private float _finalCountdownTime = 1800f; // 30 min
		private float _finalDeadlineTime = 1980f; // 33 min

		public override void _Ready()
		{
			if (GameManager.Instance != null)
			{
				_escalationTime = GameManager.Instance.EscalationTime;
				_duplicationTime = GameManager.Instance.DuplicationTime;
				_lightsOutTime = GameManager.Instance.LightsOutTime;
				_finalCountdownTime = GameManager.Instance.FinalCountdownTime;
				_finalDeadlineTime = _finalCountdownTime + 180f; // 3 minutes after countdown
			}
		}

		public override void _Process(double delta)
		{
			if (GameManager.Instance == null) return;
			
			float runElapsed = GameManager.Instance.RunElapsed;

			// 10-minute escalation
			if (!_escalationFired && runElapsed >= _escalationTime)
			{
				_escalationFired = true;
				GameManager.Instance.IsPostEscalation = true;
				GD.Print("EscalationTimer: 10-minute escalation reached. Silence immunity suspended.");
				GameManager.Instance.EmitSignal(GameManager.SignalName.EscalationReached);
			}

			// 20-minute duplication
			if (!_duplicationFired && runElapsed >= _duplicationTime)
			{
				_duplicationFired = true;
				SpawnSecondListener();
			}

			// 25-minute Lights Out
			if (!_lightsOutFired && runElapsed >= _lightsOutTime)
			{
				_lightsOutFired = true;
				TriggerLightsOut();
			}

			// 30-minute Final Countdown
			if (!_finalCountdownFired && runElapsed >= _finalCountdownTime)
			{
				_finalCountdownFired = true;
				GD.Print("EscalationTimer: 30-minute mark reached. The 3-minute final countdown begins.");
				// TODO: Notify HUD to show a 3-minute countdown
			}

			// 33-minute Deadline (3 minutes after countdown)
			if (!_finalDeadlineFired && runElapsed >= _finalDeadlineTime)
			{
				_finalDeadlineFired = true;
				if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Victory && 
					GameManager.Instance.CurrentPhase != GameManager.GamePhase.Failed)
				{
					GD.Print("EscalationTimer: 33-minute final deadline reached.");
					GameManager.Instance.TriggerRunFailed("The final deadline has passed. Radio broken.");
				}
			}
		}

		private void SpawnSecondListener()
		{
			GD.Print("EscalationTimer: 20-minute mark reached. Spawning second Listener.");
			
			var listenerScene = GD.Load<PackedScene>("res://Scenes/Listener.tscn");
			if (listenerScene == null)
			{
				GD.PushError("EscalationTimer: Could not load Listener.tscn");
				return;
			}

			var secondListener = listenerScene.Instantiate<ListenerAI>();
			secondListener.Name = "Listener2";
			
			var basementSpawn = GetTree().GetFirstNodeInGroup("ListenerSpawnBasement") as Node3D;
			Vector3 spawnPos = new Vector3(0, 1, 0);

			if (basementSpawn != null)
			{
				spawnPos = basementSpawn.GlobalPosition;
			}
			else
			{
				var listener1 = GetTree().Root.GetNodeOrNull<Node3D>("Main/Listener1");
				if (listener1 != null)
				{
					spawnPos = listener1.GlobalPosition;
				}
			}

			GameManager.Instance.AddChild(secondListener);
			secondListener.GlobalPosition = spawnPos;
			
			secondListener.SetSecondListenerMode(true);
		}

		private void TriggerLightsOut()
		{
			GD.Print("EscalationTimer: 25-minute Lights Out reached. Disabling fixed lights.");
			ClapAbility.LightsOutActive = true;
			DisableLightsInNode(GetTree().Root);
		}

		private void DisableLightsInNode(Node node)
		{
			if (node is Light3D light)
			{
				bool isPlayerLight = false;
				Node current = light.GetParent();
				while (current != null)
				{
					if (current is Godot.CharacterBody3D && current.Name.ToString().Contains("Player"))
					{
						isPlayerLight = true;
						break;
					}
					current = current.GetParent();
				}

				if (!isPlayerLight)
				{
					light.Visible = false;
				}
			}

			foreach (Node child in node.GetChildren())
			{
				DisableLightsInNode(child);
			}
		}
	}
}
