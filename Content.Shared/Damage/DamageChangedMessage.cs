using System.Collections.Generic;
using Content.Shared.Damage.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Damage
{
    public class DamageChangedMessage : ComponentMessage
    {
        public DamageChangedMessage(IDamageableComponent damageable, IReadOnlyList<DamageChangeData> data)
        {
            Damageable = damageable;
            Data = data;
        }

        public DamageChangedMessage(IDamageableComponent damageable, DamageType type, int newValue, int delta)
        {
            Damageable = damageable;

            var datum = new DamageChangeData(type, newValue, delta);
            var data = new List<DamageChangeData> {datum};

            Data = data;
        }

        /// <summary>
        ///     Reference to the <see cref="IDamageableComponent"/> that invoked the event.
        /// </summary>
        public IDamageableComponent Damageable { get; }

        /// <summary>
        ///     List containing data on each <see cref="DamageType"/> that was changed.
        /// </summary>
        public IReadOnlyList<DamageChangeData> Data { get; }

        public bool TookDamage
        {
            get
            {
                foreach (var datum in Data)
                {
                    if (datum.Delta > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
