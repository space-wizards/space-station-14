using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Alert;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Behavior;
using Content.Server.Body.Systems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
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
