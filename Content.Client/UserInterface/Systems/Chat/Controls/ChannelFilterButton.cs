using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class FilterButton : ContainerButton
    {
        private static readonly Color ColorNormal = Color.FromHex("#7b7e9e");
        private static readonly Color ColorHovered = Color.FromHex("#9699bb");
        private static readonly Color ColorPressed = Color.FromHex("#789B8C");

        private readonly TextureRect _textureRect;

        public FilterButton()
        {
            var filterTexture = IoCManager.Resolve<IResourceCache>()
                .GetTexture("/Textures/Interface/Nano/filter.svg.96dpi.png");

            // needed for same reason as ChannelSelectorButton
            Mode = ActionMode.Press;
            EnableAllKeybinds = true;

            AddChild(
                (_textureRect = new TextureRect
                {
                    Texture = filterTexture,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                })
            );

            ToggleMode = true;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            // needed since we need EnableAllKeybinds - don't double-send both UI click and Use
            if (args.Function == EngineKeyFunctions.Use) return;
            base.KeyBindDown(args);
        }

        private void UpdateChildColors()
        {
            if (_textureRect == null) return;
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    _textureRect.ModulateSelfOverride = ColorNormal;
                    break;

                case DrawModeEnum.Pressed:
                    _textureRect.ModulateSelfOverride = ColorPressed;
                    break;

                case DrawModeEnum.Hover:
                    _textureRect.ModulateSelfOverride = ColorHovered;
                    break;

                case DrawModeEnum.Disabled:
                    break;
            }
        }

        protected override void DrawModeChanged()
        {
            base.DrawModeChanged();
            UpdateChildColors();
        }

        protected override void StylePropertiesChanged()
        {
            base.StylePropertiesChanged();
            UpdateChildColors();
        }
    }
