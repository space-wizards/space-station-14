using System;
using Content.Client.Resources;
using Content.Shared.Targeting;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Targeting
{
    public sealed class TargetingDoll : VBoxContainer
    {
        private TargetingZone _activeZone = TargetingZone.Middle;
        public const string StyleClassTargetDollZone = "target-doll-zone";

        private const string TextureHigh = "/Textures/Interface/target-doll-high.svg.96dpi.png";
        private const string TextureMiddle = "/Textures/Interface/target-doll-middle.svg.96dpi.png";
        private const string TextureLow = "/Textures/Interface/target-doll-low.svg.96dpi.png";

        private readonly TextureButton _buttonHigh;
        private readonly TextureButton _buttonMiddle;
        private readonly TextureButton _buttonLow;

        public TargetingZone ActiveZone
        {
            get => _activeZone;
            set
            {
                if (_activeZone == value)
                {
                    return;
                }

                _activeZone = value;
                OnZoneChanged?.Invoke(value);

                UpdateButtons();
            }
        }

        public event Action<TargetingZone>? OnZoneChanged;

        public TargetingDoll(IResourceCache resourceCache)
        {
            _buttonHigh = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture(TextureHigh),
                StyleClasses = {StyleClassTargetDollZone},
                HorizontalAlignment = HAlignment.Center
            };

            _buttonMiddle = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture(TextureMiddle),
                StyleClasses = {StyleClassTargetDollZone},
                HorizontalAlignment = HAlignment.Center
            };

            _buttonLow = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture(TextureLow),
                StyleClasses = {StyleClassTargetDollZone},
                HorizontalAlignment = HAlignment.Center
            };

            _buttonHigh.OnPressed += _ => ActiveZone = TargetingZone.High;
            _buttonMiddle.OnPressed += _ => ActiveZone = TargetingZone.Middle;
            _buttonLow.OnPressed += _ => ActiveZone = TargetingZone.Low;

            AddChild(_buttonHigh);
            AddChild(_buttonMiddle);
            AddChild(_buttonLow);

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _buttonHigh.Pressed = _activeZone == TargetingZone.High;
            _buttonMiddle.Pressed = _activeZone == TargetingZone.Middle;
            _buttonLow.Pressed = _activeZone == TargetingZone.Low;
        }
    }
}
