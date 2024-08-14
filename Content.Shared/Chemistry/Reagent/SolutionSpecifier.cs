using System.Collections;
using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reagent;

[DataDefinition, Serializable, NetSerializable]
public partial struct SolutionSpecifier : IEnumerable<KeyValuePair<ReagentSpecifier, FixedPoint2>>,  ISerializationHooks
{
    [DataField]
    public Dictionary<ReagentSpecifier, FixedPoint2> Contents = new();

    [DataField]
    public FixedPoint2 Volume = 0;

    [DataField]
    public FixedPoint2 MaxVolume = -1;

    [DataField]
    public bool CanOverflow = false;

    [DataField]
    public bool CanReact = true;

    [DataField]
    public float Temperature = Atmospherics.T20C;
    public IEnumerator<KeyValuePair<ReagentSpecifier, FixedPoint2>> GetEnumerator() => Contents.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Contents.GetEnumerator();

    void ISerializationHooks.AfterDeserialization()
    {
        Volume = FixedPoint2.Zero;
        foreach (var (reagent, quant) in Contents)
        {
            Volume += quant;
        }
        if (MaxVolume == -1)
            MaxVolume = Volume;
    }
}
