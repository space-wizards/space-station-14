using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineComponent : Component
    {
        public override string Name => "Machine";

        public string BoardComponent { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.BoardComponent, "boardComputer", null);
        }
    }
}
