using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.CloningConsole
{
    public abstract class SharedCloningConsoleComponent : Component
    {
        [Serializable, NetSerializable]
        public sealed class CloningConsoleBoundUserInterfaceState : BoundUserInterfaceState
        {
            public readonly EntityUid? Entity;
            public readonly IReadOnlyDictionary<string, FixedPoint2> DamagePerGroup;
            public readonly IReadOnlyDictionary<string, FixedPoint2> DamagePerType;
            public readonly bool IsScanned;

            public CloningConsoleBoundUserInterfaceState(
                EntityUid? entity,
                DamageableComponent? damageable,
                bool isScanned)
            {
                Entity = entity;
                DamagePerGroup = damageable?.DamagePerGroup ?? new();
                DamagePerType = damageable?.Damage?.DamageDict ?? new();
                IsScanned = isScanned;
            }

            public bool HasDamage()
            {
                return DamagePerType.Count > 0;
            }
        }

        [Serializable, NetSerializable]
        public enum CloningConsoleUiKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public enum UiButton
        {
            Clone,
            Eject

        }

        [Serializable, NetSerializable]
        public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
        {
            public readonly UiButton Button;

            public UiButtonPressedMessage(UiButton button)
            {
                Button = button;
            }
        }
    }
}
