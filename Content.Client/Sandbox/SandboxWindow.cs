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
        public Button ShowMarkersButton { get; }
        public Button ShowBbButton { get; }

        public SandboxWindow(ILocalizationManager loc)
        {
            Resizable = false;

            Title = "Sandbox Panel";

            var vBox = new VBoxContainer { SeparationOverride = 4 };
            Contents.AddChild(vBox);

            RespawnButton = new Button { Text = loc.GetString("Respawn") };
            vBox.AddChild(RespawnButton);

            SpawnEntitiesButton = new Button { Text = loc.GetString("Spawn Entities") };
            vBox.AddChild(SpawnEntitiesButton);

            SpawnTilesButton = new Button { Text = loc.GetString("Spawn Tiles") };
            vBox.AddChild(SpawnTilesButton);

            GiveFullAccessButton = new Button { Text = loc.GetString("Give AA Id") };
            vBox.AddChild(GiveFullAccessButton);

            GiveAghostButton = new Button { Text = loc.GetString("Ghost") };
            vBox.AddChild(GiveAghostButton);

            ToggleLightButton = new Button { Text = loc.GetString("Toggle Lights") };
            vBox.AddChild(ToggleLightButton);

            ToggleSubfloorButton = new Button { Text = loc.GetString("Toggle Subfloor") };
            vBox.AddChild(ToggleSubfloorButton);

            SuicideButton = new Button { Text = loc.GetString("Suicide") };
            vBox.AddChild(SuicideButton);

            ShowMarkersButton = new Button { Text = loc.GetString("Show Spawns") };
            vBox.AddChild(ShowMarkersButton);

            ShowBbButton = new Button { Text = loc.GetString("Show Bb") };
            vBox.AddChild(ShowBbButton);
        }
    }
}
