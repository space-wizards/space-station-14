using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Components;
using Content.Shared.Medical.Blood.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Blood.Systems;

public sealed partial class VascularSystem
{
    public void ChangeBloodType(
        Entity<BloodstreamComponent, VascularSystemComponent, SolutionContainerManagerComponent?> bloodCirc,
        BloodTypePrototype newBloodType,
        BloodAntigenPolicy bloodAntigenPolicy = BloodAntigenPolicy.Overwrite)
    {
        if (bloodCirc.Comp2.BloodType! == newBloodType.ID
            || !Resolve(bloodCirc, ref bloodCirc.Comp3)
            || !_solutionSystem.TryGetSolution((bloodCirc.Owner, bloodCirc.Comp3),
                BloodstreamComponent.BloodSolutionId, out var bloodSolution, true))
            return;
        var solution = bloodSolution.Value.Comp.Solution;
        var oldReagent = solution.GetReagent(bloodCirc.Comp1.BloodReagentId);

        solution.RemoveReagent(oldReagent);
        oldReagent = new ReagentQuantity(newBloodType.WholeBloodReagent, oldReagent.Quantity, new BloodTypeDiscriminator(newBloodType));
        solution.AddReagent(oldReagent);
        bloodCirc.Comp2.BloodType = newBloodType.ID;

        UpdateAllowedAntigens((bloodCirc, bloodCirc), GetAntigensForBloodType(newBloodType), bloodAntigenPolicy);
        _solutionSystem.UpdateChemicals(bloodSolution.Value);
    }

    #region SolutionCreation

    public Solution CreateBloodSolution(BloodTypePrototype bloodType, FixedPoint2 volume)
    {
        return new Solution(bloodType.WholeBloodReagent, volume, new BloodTypeDiscriminator(bloodType));
    }
    public Solution CreateBloodCellSolution(BloodTypePrototype bloodType, FixedPoint2 volume)
    {
        return new Solution(bloodType.BloodCellsReagent, volume, new BloodTypeDiscriminator(bloodType));
    }

    public Solution CreatePlasmaSolution(BloodTypePrototype bloodType, FixedPoint2 volume)
    {
        return new Solution(bloodType.BloodPlasmaReagent, volume, new BloodTypeDiscriminator(bloodType));
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

    public void UpdateAllowedAntigens(Entity<VascularSystemComponent> bloodCirc,
        IEnumerable<ProtoId<BloodAntigenPrototype>> antigens,
        BloodAntigenPolicy antigenPolicy = BloodAntigenPolicy.Overwrite)
    {
        switch (antigenPolicy)
        {
            case BloodAntigenPolicy.KeepOriginal:
            {
                return;
            }
            case BloodAntigenPolicy.Overwrite:
            {
                bloodCirc.Comp.AllowedAntigens.Clear();
                break;
            }
        }
        foreach (var antigen in antigens)
        {
            bloodCirc.Comp.AllowedAntigens.Add(antigen);
        }
        Dirty(bloodCirc);
    }


    #endregion

    #region Setup

    private BloodTypePrototype GetInitialBloodType(Entity<VascularSystemComponent> bloodCirc, BloodDefinitionPrototype bloodDef)
    {
        return bloodCirc.Comp.BloodType == null
            ? SelectRandomizedBloodType(bloodDef)
            : _protoManager.Index<BloodTypePrototype>(bloodCirc.Comp.BloodType);
    }

    public BloodTypePrototype SelectRandomizedBloodType(ProtoId<BloodDefinitionPrototype> bloodDefProto)
    {
        return SelectRandomizedBloodType(_protoManager.Index(bloodDefProto));
    }

    public BloodTypePrototype SelectRandomizedBloodType(BloodDefinitionPrototype bloodDefProto)
    {
        var total = 0f;
        List<ProtoId<BloodTypePrototype>> items = new();
        foreach (var (bloodTypeId, chance) in bloodDefProto.BloodTypeDistribution)
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

    #region SolutionCreationOverloads

    public Solution CreateBloodSolution(Entity<VascularSystemComponent> bloodCirc,
        FixedPoint2 volume)
    {
        //We ignore the nullable here because the variable always gets filled during MapInit and you shouldn't be calling
        //this before mapInit runs
        return CreateBloodSolution(_protoManager.Index<BloodTypePrototype>(bloodCirc.Comp.BloodType!), volume);
    }

    public Solution CreateBloodSolution(ProtoId<BloodTypePrototype> bloodType, FixedPoint2 volume)
    {
        return CreateBloodSolution(_protoManager.Index(bloodType), volume);
    }



    public Solution CreatePlasmaSolution(Entity<VascularSystemComponent> bloodCirc,
        FixedPoint2 volume)
    {
        //We ignore the nullable here because the variable always gets filled during MapInit and you shouldn't be calling
        //this before mapInit runs
        return CreatePlasmaSolution(_protoManager.Index<BloodTypePrototype>(bloodCirc.Comp.BloodType!), volume);
    }

    public Solution CreatePlasmaSolution(ProtoId<BloodTypePrototype> bloodType, FixedPoint2 volume)
    {
        return CreatePlasmaSolution(_protoManager.Index(bloodType), volume);
    }

    public Solution CreateBloodCellSolution(Entity<VascularSystemComponent> bloodCirc,
        FixedPoint2 volume)
    {
        //We ignore the nullable here because the variable always gets filled during MapInit and you shouldn't be calling
        //this before mapInit runs
        return CreateBloodCellSolution(_protoManager.Index<BloodTypePrototype>(bloodCirc.Comp.BloodType!), volume);
    }

    public Solution CreateBloodCellSolution(ProtoId<BloodTypePrototype> bloodType, FixedPoint2 volume)
    {
        return CreateBloodCellSolution(_protoManager.Index(bloodType), volume);
    }

    #endregion
}
