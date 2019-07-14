using System;
using Content.Client.Utility;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface
{
    public interface IGameHud
    {
        Control RootControl { get; }

        bool EscapeButtonDown { get; set; }
        Action<bool> EscapeButtonToggled { get; set; }
        void Initialize();
    }

    /// <summary>
    ///     Responsible for laying out the default game HUD.
    /// </summary>
    internal sealed class GameHud : IGameHud
    {
        public const string StyleClassTopMenuButton = "topMenuButton";

        private TextureButton _buttonEscapeMenu;

#pragma warning disable 649
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        public void Initialize()
        {
            RootControl = new Control();

            RootControl.SetAnchorPreset(Control.LayoutPreset.Wide);

            _buttonEscapeMenu = new TextureButton
            {
                TextureNormal = _resourceCache.GetTexture("/Textures/UserInterface/hamburger.svg.96dpi.png"),
                ToggleMode = true,
                ToolTip = _localizationManager.GetString("Open escape menu.")
            };

            _buttonEscapeMenu.OnToggled += args => { EscapeButtonToggled?.Invoke(args.Pressed); };

            _buttonEscapeMenu.AddStyleClass(StyleClassTopMenuButton);

            RootControl.AddChild(_buttonEscapeMenu);
            _buttonEscapeMenu.SetAnchorAndMarginPreset(Control.LayoutPreset.TopLeft, margin: 20);
        }

        public Control RootControl { get; private set; }

        public bool EscapeButtonDown
        {
            get => _buttonEscapeMenu.Pressed;
            set => _buttonEscapeMenu.Pressed = value;
        }

        public Action<bool> EscapeButtonToggled { get; set; }
    }
}
