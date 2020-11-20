using Content.Server.Utility;
using Content.Shared.GameObjects.Components.TextureSelect;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.TextureSelect
{
    [RegisterComponent]
    public class TextureSelectComponent : SharedTextureSelectComponent, IUse
    {
        public override string Name => "TextureSelect";

        private BoundUserInterface UserInterface => Owner.GetUIOrNull(TextureSelectUiKey.Key);

        [ViewVariables(VVAccess.ReadWrite)]
        private bool _textureSelected;

        [ViewVariables]
        private List<string> _textures;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _textureSelected, "textureSelected", false);
            serializer.DataField(ref _textures, "textures", new List<string>());
        }

        public override void Initialize()
        {
            base.Initialize();
            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            UserInterface.OnReceiveMessage -= UserInterfaceOnReceiveMessage;
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case TextureSelectMessage msg:
                    {
                        SendMessage(new TextureSelectComponentMessage(msg.Texture));
                        _textureSelected = true;
                        UserInterface.CloseAll();
                        break;
                    }
                default:
                    break;
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (_textureSelected)
                return false;

            var session = eventArgs.User.PlayerSession();
            if (session != null)
                ToggleUI(session);

            return true;
        }

        private void ToggleUI(IPlayerSession session)
        {
            UserInterface.Toggle(session);
            if (UserInterface.SessionHasOpen(session))
            {
                UserInterface.SetState(new TextureSelectBoundUserInterfaceState(_textures));
            }
        }
    }

    public class TextureSelectComponentMessage : ComponentMessage
    {
        public string SelectedTexture;

        public TextureSelectComponentMessage(string selectedTexture)
        {
            SelectedTexture = selectedTexture;
        }
    }
}
