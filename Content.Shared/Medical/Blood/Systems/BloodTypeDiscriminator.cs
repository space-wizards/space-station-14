using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Blood.Systems;

[Serializable, NetSerializable]
public sealed partial class BloodTypeDiscriminator : ReagentDiscriminator
{
    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> BloodAntigens;

    [DataField(required: true)]
    public HashSet<ProtoId<BloodAntigenPrototype>> PlasmaAntigens;

    [DataField]
    public ProtoId<BloodTypePrototype>? BloodTypeId;

    //TODO: DNA

    public BloodTypeDiscriminator(BloodTypePrototype bloodType) :
        this(bloodType.BloodCellAntigens, bloodType.PlasmaAntigens, bloodType.ID)
    {
    }

    public BloodTypeDiscriminator(IEnumerable<ProtoId<BloodAntigenPrototype>> bloodAntigens,
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

    public BloodTypeDiscriminator(HashSet<ProtoId<BloodAntigenPrototype>> bloodAntigens,
        HashSet<ProtoId<BloodAntigenPrototype>> plasmaAntigens,
        ProtoId<BloodTypePrototype>? bloodTypeId = null)
    {
        BloodAntigens = [..bloodAntigens];
        PlasmaAntigens = [..plasmaAntigens];
        BloodTypeId = bloodTypeId;
    }

    public override bool Equals(ReagentDiscriminator? other)
    {
        var test = other as BloodTypeDiscriminator;
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
        return new BloodTypeDiscriminator(BloodAntigens, PlasmaAntigens, BloodTypeId);
    }

    public static implicit operator BloodTypeDiscriminator(BloodTypePrototype bloodType)
    {
        return new BloodTypeDiscriminator(bloodType);
    }
}

public enum BloodAntigenPolicy : byte
{
    Overwrite,
    KeepOriginal,
    Merge
}
