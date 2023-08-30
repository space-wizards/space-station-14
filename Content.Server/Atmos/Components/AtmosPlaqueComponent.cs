using Content.Server.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class AtmosPlaqueComponent : Component
    {
        [DataField("plaqueType")] public PlaqueType Type = PlaqueType.Unset;

        [ViewVariables(VVAccess.ReadWrite)]
        public PlaqueType TypeVV
        {
            get => Type;
            set
            {
                Type = value;
                EntitySystem.Get<AtmosPlaqueSystem>().UpdateSign(Owner, this);
            }
        }
    }
}
