using Content.Server.Construction;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachinePartComponent : Component
    {
        public override string Name => "MachinePart";

        [ViewVariables]
        public MachinePart PartType { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Rating { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.PartType, "part", MachinePart.Capacitor);
            serializer.DataField(this, x => x.Rating, "rating", 1);
        }
    }
}
