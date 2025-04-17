using Content.Shared.Damage;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Melee.Events
{
    public sealed class ShitRockedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity used to attack, for broadcast purposes.
        /// </summary>
        public EntityUid Used { get; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        /// <summary>
        /// How much damage the target was delt after all modifiers
        /// </summary>
        public DamageSpecifier? RecievedDamage { get; }

        public ShitRockedEvent(EntityUid used, EntityUid user, EntityCoordinates clickLocation, DamageSpecifier? recievedDamage)
        {
            Used = used;
            User = user;
            ClickLocation = clickLocation;
            RecievedDamage = recievedDamage;
        }
    }
}
