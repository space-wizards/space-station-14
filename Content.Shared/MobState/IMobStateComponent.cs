#nullable enable
using System.Diagnostics.CodeAnalysis;
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

        (IMobState state, int threshold)? GetEarliestIncapacitatedState(int minimumDamage);

        (IMobState state, int threshold)? GetEarliestCriticalState(int minimumDamage);

        (IMobState state, int threshold)? GetEarliestDeadState(int minimumDamage);

        (IMobState state, int threshold)? GetPreviousCriticalState(int maximumDamage);

        bool TryGetEarliestIncapacitatedState(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold);

        bool TryGetEarliestCriticalState(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold);

        bool TryGetEarliestDeadState(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold);

        bool TryGetPreviousCriticalState(
            int maximumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold);

        void UpdateState(int damage, bool syncing = false);
    }
}
