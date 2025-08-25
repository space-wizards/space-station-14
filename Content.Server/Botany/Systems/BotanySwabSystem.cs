using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Swab;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
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

            // Cross-pollenate the plants
            plant.Seed = _mutationSystem.Cross(swab.SeedData, old);

            // Transfer old plant pollen to swab
            swab.SeedData = old;

            // Copy components from the old plant to the swab
            if (old.GrowthComponents != null)
            {
                swab.components = new List<PlantGrowthComponent>();
                foreach (var component in old.GrowthComponents)
                {
                    var copiedComponent = component.DupeComponent();
                    swab.components.Add(copiedComponent);
                }
            }

            _popupSystem.PopupEntity(Loc.GetString("botany-swab-to"), args.Args.Target.Value, args.Args.User);
        }

        args.Handled = true;
    }
}

