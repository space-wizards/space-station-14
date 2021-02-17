#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Recycling
{
    [RegisterComponent]
    public class RecyclableComponent : Component
    {
        public override string Name => "Recyclable";

        /// <summary>
        ///     The prototype that will be spawned on recycle.
        /// </summary>
        [DataField("prototype")] private string? _prototype;

        /// <summary>
        ///     The amount of things that will be spawned on recycle.
        /// </summary>
        [DataField("amount")] private int _amount = 1;

        /// <summary>
        ///     Whether this is "safe" to recycle or not.
        ///     If this is false, the recycler's safety must be disabled to recycle it.
        /// </summary>
        [DataField("safe")]
        public bool Safe { get; set; } = true;

        public void Recycle(float efficiency = 1f)
        {
            if(!string.IsNullOrEmpty(_prototype))
            {
                for (var i = 0; i < Math.Max(_amount * efficiency, 1); i++)
                {
                    Owner.EntityManager.SpawnEntity(_prototype, Owner.Transform.Coordinates);
                }

            }

            Owner.Delete();
        }
    }
}
