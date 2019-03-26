using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedSpeciesComponent : Component
    {
        public sealed override string Name => "Species";

        public override uint? NetID => ContentNetIDs.SPECIES;

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
            Stand,

            /// <summary>
            ///     Mob is laying down
            /// </summary>
            Down,
        }
  
    }
}