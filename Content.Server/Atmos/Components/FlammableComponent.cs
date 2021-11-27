using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public class FlammableComponent : Component
    {
        public override string Name => "Flammable";

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
