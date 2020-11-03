using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Damage
{
    public class DamageStateChangeMessage : ComponentMessage
    {
        public DamageStateChangeMessage(IDamageableComponent damageable, DamageState state)
        {
            Damageable = damageable;
            State = state;
        }

        /// <summary>
        ///     Reference to the <see cref="IDamageableComponent"/> that invoked the event.
        /// </summary>
        public IDamageableComponent Damageable { get; }

        public DamageState State { get; }
    }
}
