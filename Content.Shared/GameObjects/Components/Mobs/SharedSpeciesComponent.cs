using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedSpeciesComponent : Component
    {
        public sealed override string Name => "Species";

        [Serializable, NetSerializable]
        public enum MobVisuals
        {
            RotationState
        }

        [Serializable, NetSerializable]
        public enum MobState
        {
            /// <summary>
            ///     Mob is standing up
            /// </summary>
            Standing,

            /// <summary>
            ///     Mob is laying down
            /// </summary>
            Down,
        }
    }
}
