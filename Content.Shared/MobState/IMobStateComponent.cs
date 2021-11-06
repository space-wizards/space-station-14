using System.Diagnostics.CodeAnalysis;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState
{
    public interface IMobStateComponent : IComponent
    {
        IMobState? CurrentState { get; }

        bool IsAlive();

        bool IsCritical();

        bool IsDead();

        bool IsIncapacitated();

        (IMobState state, FixedPoint2 threshold)? GetEarliestIncapacitatedState(FixedPoint2 minimumDamage);

        (IMobState state, FixedPoint2 threshold)? GetEarliestCriticalState(FixedPoint2 minimumDamage);

        (IMobState state, FixedPoint2 threshold)? GetEarliestDeadState(FixedPoint2 minimumDamage);

        (IMobState state, FixedPoint2 threshold)? GetPreviousCriticalState(FixedPoint2 maximumDamage);

        bool TryGetEarliestIncapacitatedState(
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold);

        bool TryGetEarliestCriticalState(
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold);

        bool TryGetEarliestDeadState(
            FixedPoint2 minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold);

        bool TryGetPreviousCriticalState(
            FixedPoint2 maximumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out FixedPoint2 threshold);

        void UpdateState(FixedPoint2 damage);
    }
}
