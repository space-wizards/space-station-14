using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedGasTileOverlaySystem : EntitySystem
{
    [Serializable, NetSerializable]
    public readonly struct SharedFireData : IEquatable<SharedFireData>
    {
        [ViewVariables] public readonly byte FireState;
        // TODO change fire color based on ByteTemp

        public SharedFireData(byte fireState)
        {
            FireState = fireState;
        }

        public bool Equals(SharedFireData other)
        {
            return FireState == other.FireState;
        }
    }
}
