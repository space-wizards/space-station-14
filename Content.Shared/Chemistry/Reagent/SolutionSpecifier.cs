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

    public List<ReagentQuantitySpecifier> Quantities
    {
        get
        {
            var list = new List<ReagentQuantitySpecifier>();
            foreach (var data in Contents)
            {
                list.Add(data);
            }
            return list;
        }
    }

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


    public SolutionSpecifier()
    {
    }

    public SolutionSpecifier(List<ReagentQuantitySpecifier> contents)
    {
        foreach (var (reagent, quant) in contents)
        {
            Contents.Add(reagent, quant);
        }
    }

    public static implicit operator SolutionSpecifier(List<ReagentQuantitySpecifier> data) => new(data);
    public static implicit operator List<ReagentQuantitySpecifier>( SolutionSpecifier s) => s.Quantities;
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
