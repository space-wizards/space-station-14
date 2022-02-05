using System;
using System.Linq;
using Content.Client.HUD.UI;
using Content.Client.Info;
using Content.Client.Resources;
using Content.Client.Targeting;
using Content.Shared.CCVar;
using Content.Shared.HUD;
using Content.Shared.Input;
using Content.Shared.Targeting;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Control = Robust.Client.UserInterface.Control;
using LC = Robust.Client.UserInterface.Controls.LayoutContainer;

namespace Content.Client.HUD
{
    /// <summary>
    ///     Responsible for laying out the default game HUD.
    /// </summary>
    public interface IGameHud : IButtonBarView
    {
        Control RootControl { get; }

        Control HandsContainer { get; }
        Control SuspicionContainer { get; }
        Control BottomLeftInventoryQuickButtonContainer { get; }
        Control BottomRightInventoryQuickButtonContainer { get; }
        Control TopInventoryQuickButtonContainer { get; }

        bool CombatPanelVisible { get; set; }
        TargetingZone TargetingZone { get; set; }
        Action<TargetingZone>? OnTargetingZoneChanged { get; set; }

        Control VoteContainer { get; }

        void AddTopNotification(TopNotification notification);

        Texture GetHudTexture(string path);

        bool ValidateHudTheme(int idx);

        // Init logic.
        void Initialize();
    }

    internal sealed partial class GameHud : IGameHud
    {
        private RulesAndInfoWindow _rulesAndInfoWindow = default!;
        private TargetingDoll _targetingDoll = default!;
        private BoxContainer _combatPanelContainer = default!;
        private BoxContainer _topNotificationContainer = default!;

        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;

        public Control HandsContainer { get; private set; } = default!;
        public Control SuspicionContainer { get; private set; } = default!;
        public Control TopInventoryQuickButtonContainer { get; private set; } = default!;
        public Control BottomLeftInventoryQuickButtonContainer { get; private set; } = default!;
        public Control BottomRightInventoryQuickButtonContainer { get; private set; } = default!;

        public bool CombatPanelVisible
        {
            get => _combatPanelContainer.Visible;
            set => _combatPanelContainer.Visible = value;
        }

        public TargetingZone TargetingZone
        {
            get => _targetingDoll.ActiveZone;
            set => _targetingDoll.ActiveZone = value;
        }
        public Action<TargetingZone>? OnTargetingZoneChanged { get; set; }

        public void AddTopNotification(TopNotification notification)
        {
            _topNotificationContainer.AddChild(notification);
        }

        public bool ValidateHudTheme(int idx)
        {
            if (!_prototypeManager.TryIndex(idx.ToString(), out HudThemePrototype? _))
            {
                Logger.ErrorS("hud", "invalid HUD theme id {0}, using different theme",
                    idx);
                var proto = _prototypeManager.EnumeratePrototypes<HudThemePrototype>().FirstOrDefault();
                if (proto == null)
                {
                    throw new NullReferenceException("No valid HUD prototypes!");
                }
                var id = int.Parse(proto.ID);
                _configManager.SetCVar(CCVars.HudTheme, id);
                return false;
            }
            return true;
        }

        public Texture GetHudTexture(string path)
        {
            var id = _configManager.GetCVar<int>("hud.theme");
            var dir = string.Empty;
            if (!_prototypeManager.TryIndex(id.ToString(), out HudThemePrototype? proto))
            {
                throw new ArgumentOutOfRangeException();
            }
            dir = proto.Path;

            var resourcePath = (new ResourcePath("/Textures/Interface/Inventory") / dir) / path;
            return _resourceCache.GetTexture(resourcePath);
        }

        public void Initialize()
        {
            RootControl = new LC { Name = "AAAAAAAAAAAAAAAAAAAAAA"};
            LC.SetAnchorPreset(RootControl, LC.LayoutPreset.Wide);

            RootControl.AddChild(GenerateButtonBar(_resourceCache, _inputManager));

            InventoryButtonToggled += down =>  TopInventoryQuickButtonContainer.Visible = down;
            InfoButtonToggled += _ => ButtonInfoOnOnToggled();

            _rulesAndInfoWindow = new RulesAndInfoWindow();
            _rulesAndInfoWindow.OnClose += () => InfoButtonDown = false;

            _inputManager.SetInputCommand(ContentKeyFunctions.OpenInfo,
                InputCmdHandler.FromDelegate(s => ButtonInfoOnOnToggled()));

            _combatPanelContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalAlignment = Control.HAlignment.Left,
                VerticalAlignment = Control.VAlignment.Bottom,
                Children =
                {
                    (_targetingDoll = new TargetingDoll(_resourceCache))
                }
            };

            LC.SetGrowHorizontal(_combatPanelContainer, LC.GrowDirection.Begin);
            LC.SetGrowVertical(_combatPanelContainer, LC.GrowDirection.Begin);
            LC.SetAnchorAndMarginPreset(_combatPanelContainer, LC.LayoutPreset.BottomRight);
            LC.SetMarginBottom(_combatPanelContainer, -10f);

            _targetingDoll.OnZoneChanged += args => OnTargetingZoneChanged?.Invoke(args);

            var centerBottomContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 5,
                HorizontalAlignment = Control.HAlignment.Center
            };
            LC.SetAnchorAndMarginPreset(centerBottomContainer, LC.LayoutPreset.CenterBottom);
            LC.SetGrowHorizontal(centerBottomContainer, LC.GrowDirection.Both);
            LC.SetGrowVertical(centerBottomContainer, LC.GrowDirection.Begin);
            LC.SetMarginBottom(centerBottomContainer, -10f);
            RootControl.AddChild(centerBottomContainer);

            HandsContainer = new BoxContainer()
            {
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Center,
                Orientation = LayoutOrientation.Vertical,
            };
            BottomRightInventoryQuickButtonContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Right
            };
            BottomLeftInventoryQuickButtonContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Left
            };
            TopInventoryQuickButtonContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Visible = false,
                VerticalAlignment = Control.VAlignment.Bottom,
                HorizontalAlignment = Control.HAlignment.Center
            };
            var bottomRow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalAlignment = Control.HAlignment.Center

            };
            bottomRow.AddChild(new Control {MinSize = (69, 0)}); //Padding (nice)
            bottomRow.AddChild(BottomLeftInventoryQuickButtonContainer);
            bottomRow.AddChild(HandsContainer);
            bottomRow.AddChild(BottomRightInventoryQuickButtonContainer);
            bottomRow.AddChild(new Control {MinSize = (1, 0)}); //Padding


            centerBottomContainer.AddChild(TopInventoryQuickButtonContainer);
            centerBottomContainer.AddChild(bottomRow);

            SuspicionContainer = new Control
            {
                HorizontalAlignment = Control.HAlignment.Center
            };

            var rightBottomContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 5
            };
            LC.SetAnchorAndMarginPreset(rightBottomContainer, LC.LayoutPreset.BottomRight);
            LC.SetGrowHorizontal(rightBottomContainer, LC.GrowDirection.Begin);
            LC.SetGrowVertical(rightBottomContainer, LC.GrowDirection.Begin);
            LC.SetMarginBottom(rightBottomContainer, -10f);
            LC.SetMarginRight(rightBottomContainer, -10f);
            RootControl.AddChild(rightBottomContainer);

            rightBottomContainer.AddChild(_combatPanelContainer);

            RootControl.AddChild(SuspicionContainer);

            LC.SetAnchorAndMarginPreset(SuspicionContainer, LC.LayoutPreset.BottomLeft,
                margin: 10);
            LC.SetGrowHorizontal(SuspicionContainer, LC.GrowDirection.End);
            LC.SetGrowVertical(SuspicionContainer, LC.GrowDirection.Begin);

            _topNotificationContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                MinSize = (600, 0)
            };
            RootControl.AddChild(_topNotificationContainer);
            LC.SetAnchorPreset(_topNotificationContainer, LC.LayoutPreset.CenterTop);
            LC.SetGrowHorizontal(_topNotificationContainer, LC.GrowDirection.Both);
            LC.SetGrowVertical(_topNotificationContainer, LC.GrowDirection.End);

            VoteContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };
            RootControl.AddChild(VoteContainer);
            LC.SetAnchorPreset(VoteContainer, LC.LayoutPreset.TopLeft);
            LC.SetMarginLeft(VoteContainer, 180);
            LC.SetMarginTop(VoteContainer, 100);
            LC.SetGrowHorizontal(VoteContainer, LC.GrowDirection.End);
            LC.SetGrowVertical(VoteContainer, LC.GrowDirection.End);
        }

        private void ButtonInfoOnOnToggled()
        {
            if (_rulesAndInfoWindow.IsOpen)
            {
                if (!_rulesAndInfoWindow.IsAtFront())
                {
                    _rulesAndInfoWindow.MoveToFront();
                    InfoButtonDown = true;
                }
                else
                {
                    _rulesAndInfoWindow.Close();
                    InfoButtonDown = false;
                }
            }
            else
            {
                _rulesAndInfoWindow.OpenCentered();
                InfoButtonDown = true;
            }
        }

        public Control RootControl { get; private set; } = default!;

        public Control VoteContainer { get; private set; } = default!;
    }
}
