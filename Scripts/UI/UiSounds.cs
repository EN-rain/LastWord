using Godot;
using LastWord;

namespace LastWord.UI
{
    /// <summary>
    /// Central UI audio helper. Wire up button hover/click/back sounds without
    /// adding AudioStreamPlayer nodes to every menu scene.
    /// </summary>
    public static class UiSounds
    {
        private const string WiredMetaKey = "_ui_sounds_wired";

        /// <summary>
        /// Connects <c>MouseEntered</c> and <c>Pressed</c> signals for a single button.
        /// </summary>
        public static void WireButton(BaseButton button)
        {
            if (button == null)
                return;

            if (button.HasMeta(WiredMetaKey) && button.GetMeta(WiredMetaKey).AsBool())
                return;

            button.MouseEntered += OnButtonHover;
            button.Pressed += OnButtonClick;
            button.SetMeta(WiredMetaKey, true);
        }

        /// <summary>
        /// Recursively wires every <see cref="BaseButton"/> under <paramref name="root"/>.
        /// Call from a menu script's <c>_Ready()</c>.
        /// </summary>
        public static void WireButtonsInNode(Node root)
        {
            if (root == null)
                return;

            WireButton(root as BaseButton);
            foreach (Node child in root.GetChildren())
                WireButtonsInNode(child);
        }

        /// <summary>
        /// Plays the configured back/cancel sound.
        /// </summary>
        public static void PlayBack(Node parent)
        {
            AudioAssets.PlayOneShot2D(AudioAssets.UiBack, parent, "UI");
        }

        /// <summary>
        /// Plays the configured error/buzzer sound.
        /// </summary>
        public static void PlayError(Node parent)
        {
            AudioAssets.PlayOneShot2D(AudioAssets.UiError, parent, "UI");
        }

        private static void OnButtonHover()
        {
            // We do not have a specific parent here, so we play on the singleton/engine level.
            AudioAssets.PlayOneShot2D(AudioAssets.UiHover, (Engine.GetMainLoop() as SceneTree)?.Root, "UI", pitchScale: (float)GD.RandRange(0.95, 1.05));
        }

        private static void OnButtonClick()
        {
            AudioAssets.PlayOneShot2D(AudioAssets.UiClick, (Engine.GetMainLoop() as SceneTree)?.Root, "UI", pitchScale: (float)GD.RandRange(0.95, 1.05));
        }
    }
}
