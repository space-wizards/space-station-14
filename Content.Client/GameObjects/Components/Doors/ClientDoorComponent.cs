using Content.Shared.GameObjects;
using Lidgren.Network;
using SS14.Client.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects
{
    public class ClientDoorComponent : SharedDoorComponent
    {
        public bool Opened { get; private set; }
        private SpriteComponent spriteComponent;

        private string OpenSprite = "Objects/door_ewo.png";
        private string CloseSprite = "Objects/door_ew.png";

        public override void Initialize()
        {
            base.Initialize();

            spriteComponent = Owner.GetComponent<SpriteComponent>();
        }

        private void Open()
        {
            Opened = true;
            spriteComponent.SetSpriteByKey(OpenSprite);
        }

        private void Close()
        {
            Opened = false;
            spriteComponent.SetSpriteByKey(CloseSprite);
        }

        public override void HandleComponentState(ComponentState state)
        {
            var castState = (DoorComponentState)state;
            if (castState.Opened == Opened)
            {
                return;
            }

            if (castState.Opened)
            {
                Open();
            }
            else
            {
                Close();
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            YamlNode node;
            if (mapping.TryGetNode("openstate", out node))
            {
                OpenSprite = node.AsString();
            }

            if (mapping.TryGetNode("closestate", out node))
            {
                CloseSprite = node.AsString();
            }
        }
    }
}
