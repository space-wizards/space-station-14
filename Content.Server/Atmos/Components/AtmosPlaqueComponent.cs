using Content.Server.Atmos.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent, ComponentProtoName("AtmosPlaque")]
    public sealed class AtmosPlaqueComponent : Component
    {
        [DataField("plaqueType")] public PlaqueType Type = PlaqueType.Unset;

        [ViewVariables(VVAccess.ReadWrite)]
        public PlaqueType TypeVV
        {
            get => Type;
            set
            {
                Type = value;
                EntitySystem.Get<AtmosPlaqueSystem>().UpdateSign(this);
            }
        }
    }
}
