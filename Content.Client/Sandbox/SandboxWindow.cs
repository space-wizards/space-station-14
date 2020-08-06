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

        public Button GiveFullAccessButton { get; } //A button that just puts a captain's ID in your hands.
        public Button GiveAghostButton { get; }
        public Button ToggleLightButton { get; }
        public Button SuicideButton { get; }
        public Button ToggleSubfloorButton { get; }

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

            GiveFullAccessButton = new Button
            {
                Text = loc.GetString("Give Full Access ID")
            };

            GiveAghostButton = new Button
            {
                Text = loc.GetString("Ghost")
            };

            ToggleLightButton = new Button
            {
                Text = loc.GetString("Toggle Lights")
            };

            ToggleSubfloorButton = new Button
            {
                Text = loc.GetString("Toggle Subfloor")
            };

            SuicideButton = new Button
            {
                Text = loc.GetString("Suicide")
            };

            Contents.AddChild(new VBoxContainer 
            {
                Children =
                {
                    RespawnButton,
                    SpawnEntitiesButton,
                    SpawnTilesButton,
                    GiveFullAccessButton,
                    GiveAghostButton,
                    ToggleLightButton,
                    ToggleSubfloorButton,
                    SuicideButton

                }
            });
        }
    }
}
