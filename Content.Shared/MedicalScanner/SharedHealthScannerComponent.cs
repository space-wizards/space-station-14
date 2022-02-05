using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.HealthScanner
{
    public abstract class SharedHealthScannerComponent : Component
    {
        public override string Name => "HealthScanner";

        [Serializable, NetSerializable]
        public class HealthScannerBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly string? TargetName;
            public readonly bool? IsAlive;
            public readonly IReadOnlyDictionary<string, FixedPoint2> DamagePerGroup;
            public readonly IReadOnlyDictionary<string, FixedPoint2> DamagePerType;

            public HealthScannerBoundUserInterfaceState(
                string? targetName,
                bool? isAlive,
                DamageableComponent? damageable)
            {
                TargetName = targetName;
                IsAlive = isAlive;
                DamagePerGroup = damageable?.DamagePerGroup ?? new();
                DamagePerType = damageable?.Damage?.DamageDict ?? new();
            }

            public bool HasDamage()
            {
                return DamagePerType.Count > 0;
            }
        }

        [Serializable, NetSerializable]
        public enum HealthScannerUiKey
        {
            Key
        }
    }
}
