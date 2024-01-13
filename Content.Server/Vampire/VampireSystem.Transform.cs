using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Vampire.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
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

    /// <summary>
    /// Convert the players body into a vampire
    /// Alternative to this would be creating a dedicated vampire race
    /// But i want the player to look 'normal' and keep the same customisations as the non vampire player
    /// </summary>
    /// <param name="vampire">Which entity to convert</param>
    private void ConvertBody(EntityUid vampire, VampireAbilityListPrototype abilityList)
    {
        var metabolizerTypes = new HashSet<string>() { "bloodsucker", "vampire" }; //Heal from drinking blood, and be damaged by drinking holy water
        //var specialDigestion = new EntityWhitelist() { Tags = new() { "Pill" } }; //Restrict Diet

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
                    _metabolism.SetMetabolizerTypes(metabolizer, metabolizerTypes);
                    _stomach.SetSpecialDigestible(stomachComponent, abilityList.AcceptableFoods);
                }
                else
                {
                    //Otherwise just add the metabolizers on
                    var tempMetabolizer = metabolizer.MetabolizerTypes ?? new HashSet<string>();
                    foreach (var t in metabolizerTypes)
                        tempMetabolizer.Add(t);

                    _metabolism.SetMetabolizerTypes(metabolizer, tempMetabolizer);
                }
            }
        }

        //Take damage from holy water splash
        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            if (reactive.ReactiveGroups == null)
                reactive.ReactiveGroups = new();

            reactive.ReactiveGroups.Add("Unholy", new() { ReactionMethod.Touch });
        }

        //Extra melee power
        if (TryComp<MeleeWeaponComponent>(vampire, out var melee))
        {
            melee.Damage = abilityList.MeleeDamage;
            melee.Animation = "WeaponArcSlash";
            melee.HitSound = new SoundPathSpecifier("/Audio/Weapons/slash.ogg");
        }
    }

    //Remove weakeness to holy items
    private void MakeImmuneToHoly(EntityUid vampire)
    {
        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            if (reactive.ReactiveGroups == null)
                return;

            reactive.ReactiveGroups.Remove("Unholy");
        }

        RemComp<UnholyComponent>(vampire);
    }
}
