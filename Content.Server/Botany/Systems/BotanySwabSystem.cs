using Content.Server.Botany.Components;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;

namespace Content.Server.Botany.Systems;

public sealed class BotanySwabSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MutationSystem _mutationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BotanySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BotanySwabComponent, DoAfterEvent>(OnDoAfter);
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

        _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, swab.SwabDelay, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnStun = true,
            NeedHand = true
        });
    }

    /// <summary>
    /// Save seed data or cross-pollenate.
    /// </summary>
    private void OnDoAfter(EntityUid uid, BotanySwabComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !TryComp<PlantHolderComponent>(args.Args.Target, out var plant) || !TryComp<BotanySwabComponent>(args.Args.Used, out var swab))
            return;

        if (swab.SeedData == null)
        {
            // Pick up pollen
            swab.SeedData = plant.Seed;
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-from"), args.Args.Target.Value, args.Args.User);
        }
        else
        {
            var old = plant.Seed;
            if (old == null)
                return;
            plant.Seed = _mutationSystem.Cross(swab.SeedData, old); // Cross-pollenate
            swab.SeedData = old; // Transfer old plant pollen to swab
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-to"), args.Args.Target.Value, args.Args.User);
        }

        args.Handled = true;
    }
}

