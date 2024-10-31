using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    /// <summary>
    /// Convert the players into a vampire, all programatic because i dont want to replace the players body
    /// </summary>
    private void MakeVampire(EntityUid vampireUid)
    {
        var vampireComponent = EnsureComp<VampireComponent>(vampireUid);
        var vampire = new Entity<VampireComponent>(vampireUid, vampireComponent);

        //Render them unable to rot, immune to pressure and thirst
        RemComp<PerishableComponent>(vampire);
        RemComp<BarotraumaComponent>(vampire);
        RemComp<ThirstComponent>(vampire); //Unsure, should vampires thirst.. or hunger?

        //Render immune to cold, but not heat
        if (TryComp<TemperatureComponent>(vampire, out var temperatureComponent))
            temperatureComponent.ColdDamageThreshold = Atmospherics.TCMB;

        MakeVulnerableToHoly(vampire);

        //Initialise currency
        vampireComponent.Balance = new() { { VampireComponent.CurrencyProto, 0 } };

        //Add the summon heirloom ability
        AddStartingAbilities(vampire);

        //Order of operation requirement, must be called after initialising balance
        UpdateBloodDisplay(vampire);
    }

    /// <summary>
    /// Add vulnerability to holy water when ingested or slashed, and take damage from the bible
    /// </summary>
    public void MakeVulnerableToHoly(Entity<VampireComponent> vampire)
    {
        //React to being beaten with the bible
        EnsureComp<UnholyComponent>(vampire);

        //Take damage from holy water splash
        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            if (reactive.ReactiveGroups == null)
                reactive.ReactiveGroups = new();

            if (!reactive.ReactiveGroups.ContainsKey("Unholy"))
            {
                reactive.ReactiveGroups.Add("Unholy", new() { ReactionMethod.Touch });
            }
        }

        if (!TryComp<BodyComponent>(vampire, out var bodyComponent))
            return;

        //Add vampire and bloodsucker to all metabolizing organs
        //And restrict diet to Pills (and liquids)
        foreach (var organ in _body.GetBodyOrgans(vampire, bodyComponent))
        {
            if (TryComp<MetabolizerComponent>(organ.Id, out var metabolizer))
            {
                if (TryComp<StomachComponent>(organ.Id, out var stomachComponent))
                {
                    //Override the stomach, prevents humans getting sick when ingesting blood
                    _metabolism.ClearMetabolizerTypes(metabolizer);
                    _stomach.SetSpecialDigestible(stomachComponent, VampireComponent.AcceptableFoods);
                }

                _metabolism.TryAddMetabolizerType(metabolizer, VampireComponent.MetabolizerVampire);
                _metabolism.TryAddMetabolizerType(metabolizer, VampireComponent.MetabolizerBloodsucker);
            }
        }
    }

    public void AddStartingAbilities(EntityUid vampire)
    {
        if (!TryComp<VampireComponent>(vampire, out var comp))
            return;
        
        foreach (var actionId in comp.BaseVampireActions)
        {
            var action = _action.AddAction(vampire, actionId);
            
            if (!action.HasValue)
                return;
            
            if (TryComp<InstantActionComponent>(action, out var instantActionComponent))
            {
                if (instantActionComponent.Event is VampireSelfPowerEvent instantActionEvent)
                {
                    comp.UnlockedPowers.Add(instantActionEvent.DefinitionName, action);
                }
            }

            if (TryComp<EntityTargetActionComponent>(action, out var entityActionComponent))
            {
                if (entityActionComponent.Event is VampireTargetedPowerEvent entityActionEvent)
                {
                    comp.UnlockedPowers.Add(entityActionEvent.DefinitionName, action);
                }
            }
        }

        UpdateBloodDisplay(vampire);
    }

    //Remove weakeness to holy items
    private void MakeImmuneToHoly(EntityUid vampire)
    {
        if (!TryComp<BodyComponent>(vampire, out var bodyComponent))
            return;

        //Add vampire and bloodsucker to all metabolizing organs
        //And restrict diet to Pills (and liquids)
        foreach (var organ in _body.GetBodyOrgans(vampire, bodyComponent))
        {
            if (TryComp<MetabolizerComponent>(organ.Id, out var metabolizer))
            {
                _metabolism.TryRemoveMetabolizerType(metabolizer, VampireComponent.MetabolizerVampire);
            }
        }

        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            if (reactive.ReactiveGroups == null)
                return;

            reactive.ReactiveGroups.Remove("Unholy");
        }

        RemComp<UnholyComponent>(vampire);
    }
}
