using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Blood.Systems;

[Serializable, NetSerializable]
public sealed partial class BloodTypeDataDescriminator : ReagentDiscriminator
{
    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> BloodAntigens;

    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> PlasmaAntigens;

    [DataField]
    public ProtoId<BloodTypePrototype>? BloodTypeId;

    //TODO: DNA

    public BloodTypeDataDescriminator(BloodTypePrototype bloodType) :
        this(bloodType.BloodCellAntigens, bloodType.PlasmaAntigens, bloodType.ID)
    {
    }

    public BloodTypeDataDescriminator(IEnumerable<ProtoId<BloodAntigenPrototype>> bloodAntigens,
        IEnumerable<ProtoId<BloodAntigenPrototype>> plasmaAntigens,
        ProtoId<BloodTypePrototype>? bloodTypeId = null)
    {
        BloodAntigens = [];
        PlasmaAntigens = [];
        foreach (var antigen in bloodAntigens)
        {
            BloodAntigens.Add(antigen);
        }
        foreach (var antigen in plasmaAntigens)
        {
            PlasmaAntigens.Add(antigen);
        }
        BloodTypeId = bloodTypeId;
    }

    public BloodTypeDataDescriminator(HashSet<ProtoId<BloodAntigenPrototype>> bloodAntigens,
        HashSet<ProtoId<BloodAntigenPrototype>> plasmaAntigens,
        ProtoId<BloodTypePrototype>? bloodTypeId = null)
    {
        BloodAntigens = [..bloodAntigens];
        PlasmaAntigens = [..plasmaAntigens];
        BloodTypeId = bloodTypeId;
    }

    public override bool Equals(ReagentDiscriminator? other)
    {
        var test = other as BloodTypeDataDescriminator;
        return test != null
               && BloodAntigens.SetEquals(test.BloodAntigens)
               && PlasmaAntigens.SetEquals(test.PlasmaAntigens)
               && BloodTypeId == test.BloodTypeId;
    }

    public override int GetHashCode()
    {
        return (BloodAntigens, PlasmaAntigens, BloodTypeId).GetHashCode();
    }

    public override ReagentDiscriminator Clone()
    {
        return new BloodTypeDataDescriminator(BloodAntigens, PlasmaAntigens, BloodTypeId);
    }

    public static implicit operator BloodTypeDataDescriminator(BloodTypePrototype bloodType)
    {
        return new BloodTypeDataDescriminator(bloodType);
    }
}

public enum BloodAntigenPolicy : byte
{
    Overwrite,
    KeepOriginal,
    Merge
}
