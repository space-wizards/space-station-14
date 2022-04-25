using System;
using System.Collections.Generic;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{

    public enum ThirstThreshold : byte
    {
        // Hydrohomies
        OverHydrated,
        Okay,
        Thirsty,
        Parched,
        Dead,
    }

    [RegisterComponent]
    public sealed class ThirstComponent : Component
    {

        // Base stuff
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("baseDecayRate")]
        public float BaseDecayRate = 0.1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ActualDecayRate;

        // Thirst
        [ViewVariables(VVAccess.ReadOnly)]
        public ThirstThreshold CurrentThirstThreshold;

        public ThirstThreshold LastThirstThreshold;

        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentThirst;

        public Dictionary<ThirstThreshold, float> ThirstThresholds { get; } = new()
        {
            {ThirstThreshold.OverHydrated, 600.0f},
            {ThirstThreshold.Okay, 450.0f},
            {ThirstThreshold.Thirsty, 300.0f},
            {ThirstThreshold.Parched, 150.0f},
            {ThirstThreshold.Dead, 0.0f},
        };

        public static readonly Dictionary<ThirstThreshold, AlertType> ThirstThresholdAlertTypes = new()
        {
            {ThirstThreshold.OverHydrated, AlertType.Overhydrated},
            {ThirstThreshold.Thirsty, AlertType.Thirsty},
            {ThirstThreshold.Parched, AlertType.Parched},
        };


        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
