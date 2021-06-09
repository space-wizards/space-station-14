#nullable enable
using Content.Shared.EffectBlocker;

namespace Content.Shared.ActionBlocker
{
    /// <summary>
    /// This interface gives components the ability to block certain actions from
    /// being done by the owning entity. For effects see <see cref="IEffectBlocker"/>
    /// </summary>
    public interface IActionBlocker
    {
        bool CanMove() => true;

        bool CanInteract() => true;

        bool CanUse() => true;

        bool CanThrow() => true;

        bool CanSpeak() => true;

        bool CanDrop() => true;

        bool CanPickup() => true;

        bool CanEmote() => true;

        bool CanAttack() => true;

        bool CanEquip() => true;

        bool CanUnequip() => true;

        bool CanChangeDirection() => true;

        bool CanShiver() => true;

        bool CanSweat() => true;
    }
}
