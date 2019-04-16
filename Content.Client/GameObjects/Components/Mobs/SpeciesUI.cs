using Content.Client.GameObjects.Components.Actor;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.Graphics.Overlays;
using Content.Shared.GameObjects;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics.Overlays;

namespace Content.Client.GameObjects
{
    /// <summary>
    /// A character UI component which shows the current damage state of the mob (living/dead)
    /// </summary>
    public class SpeciesUI : SharedSpeciesComponent, ICharacterUI
    {
        /// <summary>
        /// Holds the godot control for the species window 
        /// </summary>
        private SpeciesWindow _window;

        /// <summary>
        /// An enum representing the current state being applied to the user
        /// </summary>
        private ScreenEffects _currentEffect = ScreenEffects.None;

        // Required dependencies
#pragma warning disable 649
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IPlayerManager _playerManager;
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

            IoCManager.InjectDependencies(this);
            _window = new SpeciesWindow();

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
                    if(CurrentlyControlled)
                    {
                        ChangeHudIcon(msg);
                    }
                    break;

                case PlayerAttachedMsg _:
                    ApplyOverlay();
                    break;

                case PlayerDetachedMsg _:
                    RemoveOverlay();
                    break;
            }
        }

        private void ChangeHudIcon(HudStateChange changemessage)
        {
            _window.SetIcon(changemessage);
            SetOverlay(changemessage);
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
                var appliedeffect = EffectsDictionary[_currentEffect];
                _overlayManager.RemoveOverlay(appliedeffect.ID);
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

        private class SpeciesWindow : Control
        {
            private TextureRect _textureRect;

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Mobs/Species.tscn");

            protected override void Initialize()
            {
                base.Initialize();

                _textureRect = (TextureRect)GetChild("TextureRect");
            }

            public void SetIcon(HudStateChange changemessage)
            {
                if (!IoCManager.Resolve<IResourceCache>().TryGetResource<TextureResource>(new ResourcePath("/Textures") / changemessage.StateSprite, out var newtexture))
                {
                    Logger.Info("The Species Health Sprite {0} Does Not Exist", new ResourcePath("/Textures") / changemessage.StateSprite);
                    return;
                }

                _textureRect.Texture = newtexture;
            }
        }
    }
}
