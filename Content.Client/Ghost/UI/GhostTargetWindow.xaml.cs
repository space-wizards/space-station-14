using System.Collections.Generic;
using Content.Shared.Ghost;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Client.Ghost.UI
{
    public class GhostTargetWindow : SS14Window
    {
        private readonly IEntityNetworkManager _netManager;

        private readonly BoxContainer _buttonContainer;

        public List<string> Locations { get; set; } = new();

        public Dictionary<EntityUid, string> Players { get; set; } = new();

        public GhostTargetWindow(GhostComponent owner, IEntityNetworkManager netManager)
        {
            MinSize = SetSize = (300, 450);
            Title = Loc.GetString("ghost-target-window-title");
            _netManager = netManager;

            _buttonContainer = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                VerticalExpand = true,
                SeparationOverride = 5,

            };

            var scrollBarContainer = new ScrollContainer()
            {
                VerticalExpand = true,
                HorizontalExpand = true
            };

            scrollBarContainer.AddChild(_buttonContainer);

            Contents.AddChild(scrollBarContainer);
        }

        public void Populate()
        {
            _buttonContainer.DisposeAllChildren();
            AddButtonPlayers();
            AddButtonLocations();
        }

        private void AddButtonPlayers()
        {
            foreach (var (key, value) in Players)
            {
                var currentButtonRef = new Button
                {
                    Text = value,
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (230, 20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += (_) =>
                {
                    var msg = new GhostWarpToTargetRequestEvent(key);
                    _netManager.SendSystemNetworkMessage(msg);
                };

                _buttonContainer.AddChild(currentButtonRef);
            }
        }

        private void AddButtonLocations()
        {
            foreach (var name in Locations)
            {
                var currentButtonRef = new Button
                {
                    Text = Loc.GetString("ghost-target-window-current-button", ("name", name)),
                    TextAlign = Label.AlignMode.Right,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SizeFlagsStretchRatio = 1,
                    MinSize = (230, 20),
                    ClipText = true,
                };

                currentButtonRef.OnPressed += _ =>
                {
                    var msg = new GhostWarpToLocationRequestEvent(name);
                    _netManager.SendSystemNetworkMessage(msg);
                };

                _buttonContainer.AddChild(currentButtonRef);
            }
        }
    }
}
