using Content.Server.Disposal.Unit.EntitySystems;

namespace Content.Server.Disposal.Tube.Components
{
    [RegisterComponent]
    [Access(typeof(DisposalTubeSystem), typeof(DisposalUnitSystem))]
    public sealed partial class DisposalEntryComponent : Component
    {
        public const string HolderPrototypeId = "DisposalHolder";
    }
}
