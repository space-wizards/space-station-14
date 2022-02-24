using Content.Shared.Damage;

namespace Content.Shared.Atmos.Components
{
    // This component is in shared so that it can be used in entity-whitelists.

    [RegisterComponent]
    public sealed class FlammableComponent : Component
    {
        [ViewVariables]
        public bool Resisting = false;

        [ViewVariables]
        public readonly List<EntityUid> Collided = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OnFire { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float FireStacks { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("fireSpread")]
        public bool FireSpread { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canResistFire")]
        public bool CanResistFire { get; private set; } = false;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
