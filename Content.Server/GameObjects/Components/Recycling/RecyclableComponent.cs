using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Recycling
{
    [RegisterComponent]
    public class RecyclableComponent : Component
    {
        /// <summary>
        ///     The prototype that will be spawned on recycle.
        /// </summary>
        private string _prototype;

        /// <summary>
        ///     The amount of things that will be spawned on recycle.
        /// </summary>
        private int _amount;

        /// <summary>
        ///     Whether this is "safe" to recycle or not.
        ///     If this is false, the recycler's safety must be disabled to recycle it.
        /// </summary>
        private bool _safe;
        public override string Name => "Recyclable";

        public bool Safe => _safe;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _prototype, "prototype", string.Empty);
            serializer.DataField(ref _safe, "safe", true);
            serializer.DataField(ref _amount, "amount", 1);
        }

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
