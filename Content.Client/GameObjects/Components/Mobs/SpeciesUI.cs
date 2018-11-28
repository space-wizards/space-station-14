using Content.Shared.GameObjects;
using Content.Shared.Input;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.ResourceManagement;
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

namespace Content.Client.GameObjects
{
    public class SpeciesUI : Component
    {
        public override string Name => "Species";

        public override uint? NetID => ContentNetIDs.SPECIES;

        private SpeciesWindow _window;
        private InputCmdHandler _openMenuCmdHandler;

        public override void OnRemove()
        {
            base.OnRemove();

            _window.Dispose();
        }

        public override void OnAdd()
        {
            base.OnAdd();

            _window = new SpeciesWindow();
            _openMenuCmdHandler = InputCmdHandler.FromDelegate(session => { _window.AddToScreen(); _window.Open(); });
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            Logger.Info("WHAT {0}", message.GetType());
            var inputMgr = IoCManager.Resolve<IInputManager>();
            switch (message)
            {
                //Updates our current health sprite
                case HudStateChange msg:
                    Logger.Info("FUCK");
                    ChangeHudIcon(msg);
                    break;

                case PlayerAttachedMsg _:
                    inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, _openMenuCmdHandler);
                    break;

                case PlayerDetachedMsg _:
                    inputMgr.SetInputCommand(ContentKeyFunctions.OpenCharacterMenu, null);
                    break;
            }
        }

        private void ChangeHudIcon(HudStateChange changemessage)
        {
            _window.SetIcon(changemessage);
        }

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
                Logger.Info("ipjpopkp {0} DOES NOT EXIST", new ResourcePath("/Textures") / changemessage.StateSprite);
                if (!IoCManager.Resolve<IResourceCache>().TryGetResource<TextureResource>(new ResourcePath("/Textures") / changemessage.StateSprite, out var newtexture))
                {
                    Logger.Info("FUCK ME THE HUMAN HEALTH SPRITE {0} DOES NOT EXIST", changemessage.StateSprite);
                    return;
                }

                _textureRect.Texture = newtexture;
            }
        }
    }
}
