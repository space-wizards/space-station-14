using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Blood.Systems;

[Serializable, NetSerializable]
public sealed partial class BloodReagentData : ReagentData
{
    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> Antigens;

    [DataField]
    public ProtoId<BloodTypePrototype>? BloodTypeId;

    //TODO: DNA

    public BloodReagentData(BloodTypePrototype bloodType)
    {
        Antigens = [];
        foreach (var antigen in bloodType.PlasmaAntigens)
        {
            Antigens.Add(antigen);
        }
        foreach (var antigen in bloodType.BloodCellAntigens)
        {
            Antigens.Add(antigen);
        }
        BloodTypeId = bloodType.ID;
    }

    public BloodReagentData(IEnumerable<ProtoId<BloodAntigenPrototype>> antigens,
        ProtoId<BloodTypePrototype>? bloodTypeId = null)
    {
        Antigens = [];
        foreach (var antigen in antigens)
        {
            Antigens.Add(antigen);
        }
        BloodTypeId = bloodTypeId;
    }

    public BloodReagentData(HashSet<ProtoId<BloodAntigenPrototype>> antigens,
        ProtoId<BloodTypePrototype>? bloodTypeId = null)
    {
        Antigens = antigens;
        BloodTypeId = bloodTypeId;
    }

    public BloodReagentData(ProtoId<BloodTypePrototype>? bloodTypeId = null, params ProtoId<BloodAntigenPrototype>[] antigens)
    {
        Antigens = [];
        foreach (var antigen in antigens)
        {
            Antigens.Add(antigen);
        }
        BloodTypeId = bloodTypeId;
    }

    public override bool Equals(ReagentData? other)
    {
        var test = other as BloodReagentData;
        return test != null && Antigens.SetEquals(test.Antigens) && BloodTypeId == test.BloodTypeId;
    }

    public override int GetHashCode()
    {
        return (Antigens, BloodType: BloodTypeId).GetHashCode();
    }

    public override ReagentData Clone()
    {
        return new BloodReagentData(Antigens);
    }

    public static implicit operator BloodReagentData(BloodTypePrototype bloodType)
    {
        return new BloodReagentData(bloodType);
    }
}

public enum BloodAntigenPolicy : byte
{
    Overwrite,
    KeepOriginal,
    Merge
}
