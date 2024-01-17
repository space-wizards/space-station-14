using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Store;
using Content.Shared.Vampire.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private readonly MetabolizerSystem _metabolism = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    /// <summary>
    /// Convert the players into a vampire, all programatic because i dont want to replace the players body
    /// </summary>
    /// <param name="vampire">Which entity to convert</param>
    private void MakeVampire(EntityUid vampire)
    {
        var vampireComponent = EnsureComp<VampireComponent>(vampire);
        EnsureComp<UnholyComponent>(vampire);
        RemComp<PerishableComponent>(vampire);
        RemComp<BarotraumaComponent>(vampire);

        if (TryComp<TemperatureComponent>(vampire, out var temperatureComponent))
            temperatureComponent.ColdDamageThreshold = Atmospherics.TCMB;

        //Extra melee power
        if (TryComp<MeleeWeaponComponent>(vampire, out var melee))
        {
            melee.Damage = VampireComponent.MeleeDamage;
            melee.Animation = "WeaponArcClaw";
            melee.HitSound = new SoundPathSpecifier("/Audio/Weapons/slash.ogg");
        }

        AddStartingAbilities(vampire, vampireComponent);

        var store = EnsureComp<StoreComponent>(vampire);
        store.AccountOwner = vampire;
        _store.InitializeFromPreset(VampireComponent.StorePresetProto, vampire, store);

        MakeVulnerableToHoly(vampire);
    }
    private void MakeVulnerableToHoly(EntityUid vampire)
    {
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
                    _metabolism.SetMetabolizerTypes(metabolizer, VampireComponent.Metabolizers);
                    _stomach.SetSpecialDigestible(stomachComponent, VampireComponent.AcceptableFoods);
                }
                else
                {
                    //Otherwise just add the metabolizers on - dont want to suffocate the vampires
                    var tempMetabolizer = metabolizer.MetabolizerTypes ?? new HashSet<string>();
                    foreach (var t in VampireComponent.Metabolizers)
                        tempMetabolizer.Add(t);

                    _metabolism.SetMetabolizerTypes(metabolizer, tempMetabolizer);
                }
            }
        }
    }

    private void AddStartingAbilities(EntityUid vampire, VampireComponent component)
    {
        foreach (var ability in VampireComponent.StartingAbilities)
        {
            var action = _action.AddAction(vampire, ability);
            if (action != null)
                OnStorePurchase(vampire, action.Value);
        }
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
                var currentMetabolizers = metabolizer.MetabolizerTypes;
                if (currentMetabolizers == null)
                    continue;
                currentMetabolizers.Remove("vampire");
                _metabolism.SetMetabolizerTypes(metabolizer, currentMetabolizers);
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
