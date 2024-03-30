using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Circulatory.Components;
using Content.Shared.Medical.Circulatory.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Circulatory.Systems;

public sealed partial class BloodstreamSystem
{

    #region SolutionCreation

    public Solution CreateBloodSolution(Entity<BloodstreamComponent> bloodstream,
        FixedPoint2 volume)
    {
        //We ignore the nullable here because the variable always gets filled during MapInit and you shouldn't be calling
        //this before mapInit runs
        return CreateBloodSolution(_protoManager.Index<BloodTypePrototype>(bloodstream.Comp.BloodType!),
            _protoManager.Index<BloodDefinitionPrototype>(bloodstream.Comp.BloodDefinition), volume);
    }

    public Solution CreateBloodSolution(ProtoId<BloodTypePrototype> bloodType, ProtoId<BloodDefinitionPrototype> bloodDef,
        FixedPoint2 volume)
    {
        return CreateBloodSolution(_protoManager.Index(bloodType), _protoManager.Index(bloodDef), volume);
    }

    public Solution CreateBloodSolution(BloodTypePrototype bloodType, BloodDefinitionPrototype bloodDef,
        FixedPoint2 volume)
    {
        return new Solution(bloodDef.WholeBloodReagent, volume, new BloodReagentData(GetAntigensForBloodType(bloodType)));
    }

    public Solution CreatePlasmaSolution(Entity<BloodstreamComponent> bloodstream,
        FixedPoint2 volume)
    {
        //We ignore the nullable here because the variable always gets filled during MapInit and you shouldn't be calling
        //this before mapInit runs
        return CreatePlasmaSolution(_protoManager.Index<BloodTypePrototype>(bloodstream.Comp.BloodType!),
            _protoManager.Index<BloodDefinitionPrototype>(bloodstream.Comp.BloodDefinition), volume);
    }

    public Solution CreatePlasmaSolution(ProtoId<BloodTypePrototype> bloodType, ProtoId<BloodDefinitionPrototype> bloodDef,
        FixedPoint2 volume)
    {
        return CreatePlasmaSolution(_protoManager.Index(bloodType), _protoManager.Index(bloodDef), volume);
    }

    public Solution CreatePlasmaSolution(BloodTypePrototype bloodType, BloodDefinitionPrototype bloodDef,
        FixedPoint2 volume)
    {
        return new Solution(bloodDef.BloodPlasmaReagent, volume, new BloodReagentData(GetPlasmaAntigensForBloodType(bloodType)));
    }

    public Solution CreateBloodCellSolution(Entity<BloodstreamComponent> bloodstream,
        FixedPoint2 volume)
    {
        //We ignore the nullable here because the variable always gets filled during MapInit and you shouldn't be calling
        //this before mapInit runs
        return CreateBloodCellSolution(_protoManager.Index<BloodTypePrototype>(bloodstream.Comp.BloodType!),
            _protoManager.Index<BloodDefinitionPrototype>(bloodstream.Comp.BloodDefinition), volume);
    }

    public Solution CreateBloodCellSolution(ProtoId<BloodTypePrototype> bloodType, ProtoId<BloodDefinitionPrototype> bloodDef,
        FixedPoint2 volume)
    {
        return CreateBloodCellSolution(_protoManager.Index(bloodType), _protoManager.Index(bloodDef), volume);
    }

    public Solution CreateBloodCellSolution(BloodTypePrototype bloodType, BloodDefinitionPrototype bloodDef,
        FixedPoint2 volume)
    {
        return new Solution(bloodDef.BloodCellsReagent, volume, new BloodReagentData(GetBloodCellAntigensForBloodType(bloodType)));
    }

    #endregion

    #region AntigenLogic

    public IEnumerable<ProtoId<BloodAntigenPrototype>> GetBloodCellAntigensForBloodType(BloodTypePrototype bloodType)
    {
        foreach (var antigen in bloodType.BloodCellAntigens)
        {
            yield return antigen.Id;
        }
    }

    public IEnumerable<ProtoId<BloodAntigenPrototype>> GetPlasmaAntigensForBloodType(BloodTypePrototype bloodType)
    {
        foreach (var antigen in bloodType.PlasmaAntigens)
        {
            yield return antigen.Id;
        }
    }

    public IEnumerable<ProtoId<BloodAntigenPrototype>> GetAntigensForBloodType(BloodTypePrototype bloodType)
    {
        foreach (var value in GetBloodCellAntigensForBloodType(bloodType))
        {
            yield return value;
        }
        foreach (var value in GetPlasmaAntigensForBloodType(bloodType))
        {
            yield return value;
        }
    }

    public void AddAllowedAntigens(Entity<BloodstreamComponent> bloodstream,
        IEnumerable<ProtoId<BloodAntigenPrototype>> antigens)
    {
        foreach (var antigen in antigens)
        {
            bloodstream.Comp.AllowedAntibodies.Add(antigen);
        }
        Dirty(bloodstream);
    }


    #endregion

    #region Setup

    private BloodTypePrototype GetInitialBloodType(Entity<BloodstreamComponent> bloodstream, BloodDefinitionPrototype bloodDef)
    {
        return bloodstream.Comp.BloodType == null
            ? SelectRandomizedBloodType(bloodDef)
            : _protoManager.Index<BloodTypePrototype>(bloodstream.Comp.BloodType);
    }

    public BloodTypePrototype SelectRandomizedBloodType(BloodDefinitionPrototype bloodDefProto)
    {
        var total = 0f;
        List<ProtoId<BloodTypePrototype>> items = new();
        foreach (var (chance, bloodTypeId) in bloodDefProto.BloodTypeDistribution)
        {
            total += chance.Float();
            items.Add(bloodTypeId);
        }
        var perItemIncrease = total / items.Count;
        var random = _random.NextFloat(0, total);
        var foundProtoId = items[(int) MathF.Floor(random / perItemIncrease)];
        return _protoManager.Index(foundProtoId);
    }

    #endregion


}
