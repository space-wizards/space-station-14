using System;
using Content.Shared.EffectBlocker;

namespace Content.Shared.ActionBlocker
{
    /// <summary>
    /// This interface gives components the ability to block certain actions from
    /// being done by the owning entity. For effects see <see cref="IEffectBlocker"/>
    /// </summary>
    [Obsolete("Use events instead")]
    public interface IActionBlocker
    {
        [Obsolete("Use MoveAttemptEvent instead")]
        bool CanMove() => true;

        [Obsolete("Use InteractAttemptEvent instead")]
        bool CanInteract() => true;

        [Obsolete("Use UseAttemptEvent instead")]
        bool CanUse() => true;

        [Obsolete("Use ThrowAttemptEvent instead")]
        bool CanThrow() => true;

        [Obsolete("Use SpeakAttemptEvent instead")]
        bool CanSpeak() => true;

        [Obsolete("Use DropAttemptEvent instead")]
        bool CanDrop() => true;

        [Obsolete("Use PickupAttemptEvent instead")]
        bool CanPickup() => true;

        [Obsolete("Use EmoteAttemptEvent instead")]
        bool CanEmote() => true;

        [Obsolete("Use AttackAttemptEvent instead")]
        bool CanAttack() => true;

        [Obsolete("Use EquipAttemptEvent instead")]
        bool CanEquip() => true;

        [Obsolete("Use UnequipAttemptEvent instead")]
        bool CanUnequip() => true;

        [Obsolete("Use ChangeDirectionAttemptEvent instead")]
        bool CanChangeDirection() => true;

        [Obsolete("Use ShiverAttemptEvent instead")]
        bool CanShiver() => true;

        [Obsolete("Use SweatAttemptEvent instead")]
        bool CanSweat() => true;
    }
}
