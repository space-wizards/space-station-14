using Content.Client.GameObjects.Components.Actor;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Graphics.Overlays;
using Content.Shared.GameObjects;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using System.Collections.Generic;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.GameObjects.Components.Renderable;

namespace Content.Client.GameObjects
{
    /// <summary>
    /// A character UI component which shows the current damage state of the mob (living/dead)
    /// </summary>
    public class SpeciesUI : SharedSpeciesComponent, ICharacterUI
    {
        private StatusEffectsUI _ui;

        /// <summary>
        /// Holds the godot control for the species window 
        /// </summary>
        private SpeciesWindow _window;

        /// <summary>
        /// An enum representing the current state being applied to the user
        /// </summary>
        private ScreenEffects _currentEffect = ScreenEffects.None;

#pragma warning disable 649
        // Required dependencies
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
        [Dependency] private readonly IResourceCache _resourceCache;
#pragma warning restore 649

        //Relevant interface implementation for the character UI controller
        public Control Scene => _window;
        public UIPriority Priority => UIPriority.Species;

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        private bool CurrentlyControlled => _playerManager.LocalPlayer.ControlledEntity == Owner;

        /// <summary>
        /// Holds the screen effects that can be applied mapped ot their relevant overlay
        /// </summary>
        private Dictionary<ScreenEffects, Overlay> EffectsDictionary;

        public override void OnRemove()
        {
            base.OnRemove();

            _window.Dispose();
        }

        public override void OnAdd()
        {
            base.OnAdd();

            _window = new SpeciesWindow();
            _ui = new StatusEffectsUI();

            EffectsDictionary = new Dictionary<ScreenEffects, Overlay>()
            {
                { ScreenEffects.CircleMask, new CircleMaskOverlay() },
                { ScreenEffects.GradientCircleMask, new GradientCircleMask() }
            };
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            switch (message)
            {
                case HudStateChange msg:
                    if (CurrentlyControlled)
                    {
                        ChangeHudIcon(msg);
                    }
                    break;

                case PlayerAttachedMsg _:
                    _ui.Parent?.RemoveChild(_ui);

                    _userInterfaceManager.StateRoot.AddChild(_ui);
                    ApplyOverlay();
                    break;

                case PlayerDetachedMsg _:
                    _ui.Parent?.RemoveChild(_ui);
                    RemoveOverlay();
                    break;
            }
        }

        private void ChangeHudIcon(HudStateChange changeMessage)
        {
            var path = SharedSpriteComponent.TextureRoot / changeMessage.StateSprite;
            var texture = _resourceCache.GetTexture(path);

            _window.SetIcon(texture);
            _ui.SetHealthIcon(texture);

            SetOverlay(changeMessage);
        }

        private void SetOverlay(HudStateChange message)
        {
            RemoveOverlay();

            _currentEffect = message.effect;

            ApplyOverlay();
        }

        private void RemoveOverlay()
        {
            if (_currentEffect != ScreenEffects.None)
            {
                var appliedEffect = EffectsDictionary[_currentEffect];
                _overlayManager.RemoveOverlay(appliedEffect.ID);
            }

            _currentEffect = ScreenEffects.None;
        }

        private void ApplyOverlay()
        {
            if (_currentEffect != ScreenEffects.None)
            {
                var overlay = EffectsDictionary[_currentEffect];
                if (_overlayManager.HasOverlay(overlay.ID))
                {
                    return;
                }
                _overlayManager.AddOverlay(overlay);
            }
        }

        private class SpeciesWindow : TextureRect
        {
            public SpeciesWindow()
            {
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                SizeFlagsVertical = SizeFlags.None;

                Texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Mob/UI/Human/human0.png");
            }

            public void SetIcon(Texture texture)
            {
                Texture = texture;
            }
        }
    }
}
