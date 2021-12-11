using System.Collections.Generic;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Friend(typeof(RespiratorSystem))]
    public class RespiratorComponent : Component
    {
        public override string Name => "Respirator";

        [ViewVariables]
        [DataField("needsGases")]
        public Dictionary<Gas, float> NeedsGases { get; set; } = new();

        [ViewVariables]
        [DataField("producesGases")]
        public Dictionary<Gas, float> ProducesGases { get; set; } = new();

        [ViewVariables]
        [DataField("deficitGases")]
        public Dictionary<Gas, float> DeficitGases { get; set; } = new();

        [ViewVariables] public bool Suffocating { get; set; }

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("damageRecovery", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageRecovery = default!;

        public float AccumulatedFrametime;
    }
}
