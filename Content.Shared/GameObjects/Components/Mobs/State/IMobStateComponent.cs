#nullable enable
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public interface IMobStateComponent : IComponent
    {
        IMobState? CurrentState { get; }

        bool IsAlive();

        bool IsCritical();

        bool IsDead();

        bool IsIncapacitated();

        (IMobState state, int threshold)? GetEarliestIncapacitatedState(int minimumDamage);

        bool TryGetEarliestIncapacitatedState(
            int minimumDamage,
            [NotNullWhen(true)] out IMobState? state,
            out int threshold);

        void UpdateState(int damage, bool syncing = false);
    }
}
