using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.EntitySystems;

public abstract partial class SharedGasTileOverlaySystem : EntitySystem
{
    /// <summary>
    ///     array of the ids of all visible gases.
    /// </summary>
    public int[] VisibleGasId = default!;

    public override void Initialize()
    {
        base.Initialize();

        List<int> visibleGases = new();

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gasPrototype = _atmosphere.GetGas(i);
            if (gasPrototype.GasOverlaySprite != null)
                visibleGases.Add(i);
        }
        VisibleGasId = visibleGases.ToArray();
    }

    [Serializable, NetSerializable]
    public readonly struct SharedVisibleGasData : IEquatable<SharedVisibleGasData>
    {
        [ViewVariables] public readonly byte[] Opacity;

        public SharedVisibleGasData(byte[] opacity)
        {
            Opacity = opacity;
        }

        public bool Equals(SharedVisibleGasData other)
        {
            if (Opacity?.Length != other.Opacity?.Length)
                return false;

            if (Opacity != null && other.Opacity != null)
            {
                for (var i = 0; i < Opacity.Length; i++)
                {
                    if (Opacity[i] != other.Opacity[i])
                        return false;
                }
            }

            return true;
        }
    }
}
