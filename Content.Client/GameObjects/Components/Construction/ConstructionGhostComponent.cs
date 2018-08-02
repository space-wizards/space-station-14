using Content.Shared.Construction;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;

namespace Content.Client.GameObjects.Components.Construction
{
    public class ConstructionGhostComponent : Component
    {
        public override string Name => "ConstructionGhost";

        public ConstructionPrototype Prototype { get; set; }
        public ConstructorComponent Master { get; set; }
        public int GhostID { get; set; }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case ClientEntityClickMsg clickMsg:
                    Master.TryStartConstruction(GhostID);
                    break;
            }
        }
    }
}
