using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Circulatory.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Circulatory.Systems;

public sealed partial class BloodReagentData : ReagentData
{
    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> Antigens;

    [DataField]
    public ProtoId<BloodTypePrototype>? BloodType;

    public BloodReagentData(IEnumerable<ProtoId<BloodAntigenPrototype>> antigens,
        ProtoId<BloodTypePrototype>? bloodType = null)
    {
        Antigens = [];
        foreach (var antigen in antigens)
        {
            Antigens.Add(antigen);
        }
        BloodType = bloodType;
    }

    public BloodReagentData(HashSet<ProtoId<BloodAntigenPrototype>> antigens,
        ProtoId<BloodTypePrototype>? bloodType = null)
    {
        Antigens = antigens;
        BloodType = bloodType;
    }

    public BloodReagentData(ProtoId<BloodTypePrototype>? bloodType = null, params ProtoId<BloodAntigenPrototype>[] antigens)
    {
        Antigens = [];
        foreach (var antigen in antigens)
        {
            Antigens.Add(antigen);
        }
        BloodType = bloodType;
    }

    public override bool Equals(ReagentData? other)
    {
        var test = other as BloodReagentData;
        return test != null && Antigens.SetEquals(test.Antigens) && BloodType == test.BloodType;
    }

    public override int GetHashCode()
    {
        return (Antigens, BloodType).GetHashCode();
    }

    public override ReagentData Clone()
    {
        return new BloodReagentData(Antigens);
    }
}
