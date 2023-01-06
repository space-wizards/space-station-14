using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Recycling
{
    [NetSerializable, Serializable]
    public enum RecyclerVisuals
    {
        Bloody
    }

    [NetworkedComponent]
    public abstract class SharedRecyclerComponent : Component
    {
        /// <summary>
        /// Default sound to play when recycling
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("sound")]
        public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/saw.ogg");

        // Ratelimit sounds to avoid spam
        public TimeSpan LastSound;

        // The number of items that have been recycled by this recycler
        public int ItemsProcessed;
    }
}
