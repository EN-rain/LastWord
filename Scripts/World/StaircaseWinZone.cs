using Godot;
using LastWord;

namespace LastWord.World
{
    public partial class StaircaseWinZone : Area3D
    {
        public override void _Ready()
        {
            base._Ready();
            BodyEntered += OnBodyEntered;
        }

        private void OnBodyEntered(Node3D body)
        {
            if (GameManager.Instance == null)
                return;

            if (body is not PlayerController player)
                return;

            bool sequenceComplete = GameManager.Instance.CurrentPhase == GameManager.GamePhase.Phase3
                || GameManager.Instance.CurrentPhase == GameManager.GamePhase.Victory;

            if (!sequenceComplete)
            {
                var sequenceManager = GameManager.Instance.GetNodeOrNull<SequenceManager>(GameManager.Instance.SequenceManagerPath);
                if (sequenceManager != null)
                    sequenceComplete = sequenceManager.IsComplete;
            }

            if (!sequenceComplete)
                return;

            GD.Print($"StaircaseWinZone: Player {player.Name} entered the staircase win zone; triggering final broadcast.");
            GameManager.Instance.OnFinalBroadcastTransmitted(player);
        }
    }
}
