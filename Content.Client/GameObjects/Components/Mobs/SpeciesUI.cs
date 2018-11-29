using Content.Client.Graphics.Overlays;
using Content.Shared.GameObjects;
using Content.Shared.Input;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Graphics.Overlays;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Player;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System.Collections.Generic;

namespace Content.Client.GameObjects
{
    public class SpeciesUI : Component
    {
        public override string Name => "Species";

        public override uint? NetID => ContentNetIDs.SPECIES;

        private SpeciesWindow _window;
        private InputCmdHandler _openMenuCmdHandler;
        private ScreenEffects _currentEffect = ScreenEffects.None;

        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IPlayerManager _playerManager;

        private bool CurrentlyControlled => _playerManager.LocalPlayer.ControlledEntity == Owner;

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
            _openMenuCmdHandler = InputCmdHandler.FromDelegate(session => { _window.AddToScreen(); _window.Open(); });

            EffectsDictionary = new Dictionary<ScreenEffects, IOverlay>()
            {
                { ScreenEffects.CircleMask, new CircleMaskOverlay() },
                { ScreenEffects.GradientCircleMask, new GradientCircleMask() }
            };
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            var inputMgr = IoCManager.Resolve<IInputManager>();
            switch (message)
            {
                case HudStateChange msg:
                    if(CurrentlyControlled)
                    {
                        ChangeHudIcon(msg);
                    }
                    break;

                case PlayerAttachedMsg _:
                    inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, _openMenuCmdHandler);
                    ApplyOverlay();
                    break;

                case PlayerDetachedMsg _:
                    inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
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
                _overlayManager.RemoveOverlay(nameof(appliedeffect));
            }

            _currentEffect = ScreenEffects.None;
        }

        private void ApplyOverlay()
        {
            if (_currentEffect != ScreenEffects.None)
            {
                _overlayManager.AddOverlay(EffectsDictionary[_currentEffect]);
            }
        }

        private Dictionary<ScreenEffects, IOverlay> EffectsDictionary;

        private class SpeciesWindow : SS14Window
        {
            private TextureRect _textureRect;

            protected override ResourcePath ScenePath => new ResourcePath("/Scenes/Mobs/Species.tscn");

            protected override void Initialize()
            {
                base.Initialize();

                _textureRect = (TextureRect)Contents.GetChild("Control").GetChild("TextureRect");
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
