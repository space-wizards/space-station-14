using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Swab;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using System.Linq;
using System.Collections.Generic;

namespace Content.Server.Botany.Systems;

public sealed class BotanySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MutationSystem _mutationSystem = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BotanySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BotanySwabComponent, BotanySwabDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// This handles swab examination text
    /// so you can tell if they are used or not.
    /// </summary>
    private void OnExamined(EntityUid uid, BotanySwabComponent swab, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (swab.SeedData != null)
                args.PushMarkup(Loc.GetString("swab-used"));
            else
                args.PushMarkup(Loc.GetString("swab-unused"));
        }
    }

    /// <summary>
    /// Handles swabbing a plant.
    /// </summary>
    private void OnAfterInteract(EntityUid uid, BotanySwabComponent swab, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantHolderComponent>(args.Target))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.SwabDelay, new BotanySwabDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// Save seed data or cross-pollenate.
    /// </summary>
    private void OnDoAfter(EntityUid uid, BotanySwabComponent swab, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !TryComp<PlantHolderComponent>(args.Args.Target, out var plant))
            return;

        if (swab.SeedData == null)
        {
            // Pick up pollen
            swab.SeedData = plant.Seed;
            if (plant.Seed != null)
            {
                // Deep copy components to prevent changes to the original plant from affecting the swab
                swab.components = new List<PlantGrowthComponent>();
                foreach (var component in plant.Seed.GrowthComponents)
                {
                    var copiedComponent = component.DupeComponent();
                    swab.components.Add(copiedComponent);
                }
            }

            _popupSystem.PopupEntity(Loc.GetString("botany-swab-from"), args.Args.Target.Value, args.Args.User);
        }
        else
        {
            var old = plant.Seed;
            if (old == null)
                return;

            for (int index = 0; index < old.GrowthComponents.Count(); index++)
            {
                var c1 = old.GrowthComponents[index];
                foreach (var c2 in swab.components)
                {
                    if (c1.GetType() == c2.GetType())
                    {
                        if (_random.Prob(0.5f))
                            _serializationManager.CopyTo(c2, ref c1, notNullableOverride: true);
                    }
                }
            }

            //Growth Components mean that those systems listed for the BotanySwabDoAfterEvent and do their stuff.
            //Wait, no. If the swab has components the plant doesn't that wont fire off.
            //I DO need to check that here (though maybe I can get away with calling an event for it instead of looking up systems?
            //Or maybe some override function that only applies the Cross math (since the other effect is copying the component?)

            //OR OR MAYBE THIS: the event fires, this handles  ones that aren't on the plant, and those system handle the event for ones that do?
            //that might be more work?
            var plantcomps = EntityManager.GetComponents<PlantGrowthComponent>(args.Args.Target.Value);
            foreach (var gc in swab.components)
            {
                if (plantcomps.Any(p => p.GetType() == gc.GetType()))
                {
                    //fire the event or override to handle the 50% variable swap test.
                }
                else
                {
                    //copy the component over on the 50% chance.
                    if (_random.Prob(0.5f)) {
                        //EntityManager.add
                    }
                }
            }


            plant.Seed = _mutationSystem.Cross(swab.SeedData, old); // Cross-pollenate
            swab.SeedData = old; // Transfer old plant pollen to swab
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-to"), args.Args.Target.Value, args.Args.User);
        }

        args.Handled = true;
    }
}

