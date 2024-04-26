using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Blood.Systems;

[Serializable, NetSerializable]
public sealed partial class BloodReagentData : ReagentData
{
    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> BloodAntigens;

    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> PlasmaAntigens;

    [DataField]
    public ProtoId<BloodTypePrototype>? BloodTypeId;

    [DataField]
    public float PlasmaPercentage;

    //TODO: DNA

    public BloodReagentData(BloodTypePrototype bloodType) :
        this(bloodType.BloodCellAntigens, bloodType.PlasmaAntigens, bloodType.ID, bloodType.PlasmaPercentage)
    {
    }

    public BloodReagentData(IEnumerable<ProtoId<BloodAntigenPrototype>> bloodAntigens,
        IEnumerable<ProtoId<BloodAntigenPrototype>> plasmaAntigens,
        ProtoId<BloodTypePrototype>? bloodTypeId = null,
        float plasmaPercentage = 0.55f)
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
        PlasmaPercentage = plasmaPercentage;
    }

    public BloodReagentData(HashSet<ProtoId<BloodAntigenPrototype>> bloodAntigens,
        HashSet<ProtoId<BloodAntigenPrototype>> plasmaAntigens,
        ProtoId<BloodTypePrototype>? bloodTypeId = null,
        float plasmaPercentage = 0.55f)
    {
        BloodAntigens = [..bloodAntigens];
        PlasmaAntigens = [..plasmaAntigens];
        BloodTypeId = bloodTypeId;
        PlasmaPercentage = plasmaPercentage;
    }

    public override bool Equals(ReagentData? other)
    {
        var test = other as BloodReagentData;
        //Do not check plasma percentage because it will be changed and should not be used to differentiate between
        //different blood types. I hate this, but it's the only way to store data that changes on reagents
        //without rewriting the entire reagents/reagentData api
        return test != null
               && BloodAntigens.SetEquals(test.BloodAntigens)
               && PlasmaAntigens.SetEquals(test.PlasmaAntigens)
               && BloodTypeId == test.BloodTypeId;
    }

    public override int GetHashCode()
    {
        //Do not check plasma percentage because it will be changed and should not be used to differentiate between
        //different blood types. I hate this, but it's the only way to store data that changes on reagents
        //without rewriting the entire reagents/reagentData api
        return (BloodAntigens, PlasmaAntigens, BloodTypeId).GetHashCode();
    }

    public override ReagentData Clone()
    {
        return new BloodReagentData(BloodAntigens, PlasmaAntigens, BloodTypeId, PlasmaPercentage);
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
