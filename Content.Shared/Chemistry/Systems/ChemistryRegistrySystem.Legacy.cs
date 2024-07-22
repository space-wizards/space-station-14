using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Chemistry.Systems;

public sealed partial class ChemistryRegistrySystem
{
    private const float DefaultMolarMass = 18;
    private void ConvertLegacyReagentPrototypes(ref Dictionary<string, EntityUid> pendingReagents)
    {
        var i = 0;
        foreach (var reagentProto in _protoManager.EnumeratePrototypes<ReagentPrototype>())
        {
            var newEnt = Spawn();
            var reagentDef = AddComp<ReagentDefinitionComponent>(newEnt);

            reagentDef.NameLocId = reagentProto.NameLocId;
            reagentDef.MolarMass = DefaultMolarMass;
            reagentDef.Recognizable = reagentProto.Recognizable;
            reagentDef.PricePerUnit = reagentProto.PricePerUnit;
            reagentDef.Flavor = reagentProto.Flavor;
            reagentDef.DescriptionLocId = reagentProto.DescriptionLocId;
            reagentDef.PhysicalDescriptionLocId = reagentProto.PhysicalDescriptionLocId;
            reagentDef.FlavorMinimum = reagentProto.FlavorMinimum;
            reagentDef.SubstanceColor = reagentProto.SubstanceColor;
            reagentDef.SpecificHeat = reagentProto.SpecificHeat;
            reagentDef.BoilingPoint = reagentProto.BoilingPoint;
            reagentDef.MeltingPoint = reagentProto.MeltingPoint;
            reagentDef.Slippery = reagentProto.Slippery;
            reagentDef.Fizziness = reagentProto.Fizziness;
            reagentDef.Viscosity = reagentProto.Viscosity;
            reagentDef.FootstepSound = reagentProto.FootstepSound;
            reagentDef.WorksOnTheDead = reagentProto.WorksOnTheDead;
            reagentDef.Metabolisms = reagentProto.Metabolisms;
            reagentDef.ReactiveEffects = reagentProto.ReactiveEffects;
            reagentDef.TileReactions = reagentProto.TileReactions;
            reagentDef.PlantMetabolisms = reagentProto.PlantMetabolisms;

            if (reagentProto.MetamorphicSprite != null)
            {
                var reagentMetaMorph = AddComp<ReagentMetamorphicSpriteComponent>(newEnt);
                reagentMetaMorph.MetamorphicSprite = reagentProto.MetamorphicSprite;
                reagentMetaMorph.MetamorphicMaxFillLevels = reagentProto.MetamorphicMaxFillLevels;
                reagentMetaMorph.MetamorphicFillBaseName = reagentProto.MetamorphicFillBaseName;
                reagentMetaMorph.MetamorphicChangeColor = reagentProto.MetamorphicChangeColor;
            }
            _metaSystem.SetEntityName(newEnt, reagentProto.ID);
            _metaSystem.SetEntityDescription(newEnt, reagentProto.LocalizedDescription);
            pendingReagents.Add(reagentProto.ID, newEnt);
            i++;
        }
        Log.Info($"{i} legacy reagents loaded");
    }
    private void ConvertLegacyReactionPrototypes(ref Dictionary<string, EntityUid> pendingReactions)
    {
        var i = 0;
        foreach (var reactionProto in _protoManager.EnumeratePrototypes<ReactionPrototype>())
        {
            var newEnt = Spawn();
            var reactionDef = AddComp<ReactionDefinitionComponent>(newEnt);
            var tempReq = AddComp<RequiresReactionTemperatureComponent>(newEnt);
            tempReq.MinimumTemperature = reactionProto.MinimumTemperature;
            tempReq.MaximumTemperature = reactionProto.MaximumTemperature;
            reactionDef.ConserveEnergy = reactionProto.ConserveEnergy;
            reactionDef.Effects = reactionProto.Effects;
            reactionDef.Impact = reactionProto.Impact;
            reactionDef.Sound = reactionProto.Sound;
            reactionDef.Quantized = reactionProto.Quantized;
            reactionDef.Priority = reactionProto.Priority;
            reactionDef.LegacyId = reactionProto.ID;

            reactionDef.Reactants = new();
            foreach (var (reagentId, data) in reactionProto.Reactants)
            {
                reactionDef.Reactants.Add(reagentId, new ReactantData(data.Amount, data.Catalyst));
            }

            if (reactionProto.MixingCategories != null)
            {
                var mixingReq = AddComp<RequiresReactionMixingComponent>(newEnt);
                mixingReq.MixingCategories = reactionProto.MixingCategories;
            }

            _metaSystem.SetEntityName(newEnt, reactionProto.Name);
            pendingReactions.Add(reactionProto.ID, newEnt);
            i++;
        }
        Log.Info($"{i} legacy reactions loaded");
    }
}
