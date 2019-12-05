using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;

namespace Content.Client.Sandbox
{
    public sealed class SandboxWindow : SS14Window
    {
        public Button RespawnButton { get; }
        public Button SpawnEntitiesButton { get; }
        public Button SpawnTilesButton { get; }

        public SandboxWindow(ILocalizationManager loc)
        {
            Title = loc.GetString("Sandbox Panel");

            RespawnButton = new Button
            {
                Text = loc.GetString("Respawn")
            };

            SpawnEntitiesButton = new Button
            {
                Text = loc.GetString("Spawn Entities")
            };

            SpawnTilesButton = new Button
            {
                Text = loc.GetString("Spawn Tiles")
            };

            Contents.AddChild(new VBoxContainer
            {
                Children =
                {
                    RespawnButton,
                    SpawnEntitiesButton,
                    SpawnTilesButton
                }
            });
        }
    }
}
