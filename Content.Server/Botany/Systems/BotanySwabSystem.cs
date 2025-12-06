using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Swab;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Botany.Systems;

public sealed class BotanySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MutationSystem _mutationSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BotanySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BotanySwabComponent, BotanySwabDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BotanySwabComponent, UseInHandEvent>(OnClean);
        SubscribeLocalEvent<BotanySwabComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<BotanySwabComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<BotanySwabComponent, EntGotRemovedFromContainerMessage>(OnRemove);
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
            else if (swab.UsableIfClean) //if swab can only be used when dirty, it doesn't have an unused state, so don't display this.
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

        if (!swab.UsableIfClean && swab.SeedData == null) //if swab is not usable when clean, and is clean (has no seedData), prevent use.
        {
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-unusable"), uid, args.User);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.SwabDelay, new BotanySwabDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// Save seed data or cross-pollinate.
    /// </summary>
    private void OnDoAfter(EntityUid uid, BotanySwabComponent swab, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !TryComp<PlantHolderComponent>(args.Args.Target, out var plant))
            return;

        _audioSystem.PlayPvs(swab.SwabSound, uid);

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

            plant.Seed = _mutationSystem.Cross(swab.SeedData, old); // Cross-pollinate

            if (swab.Contaminate)
                swab.SeedData = old;// Transfer old plant pollen to swab if contamination is allowed

            _popupSystem.PopupEntity(Loc.GetString("botany-swab-to"), args.Args.Target.Value, args.Args.User);
        }
        args.Handled = true;
    }

    ///<summary>
    /// Remove a swab's SeedData
    /// </summary>
    private void OnClean(EntityUid uid, BotanySwabComponent swab, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!swab.Cleanable)
            return;

        swab.SeedData = null;
        _popupSystem.PopupEntity(Loc.GetString("botany-swab-clean"), uid, args.User);
        _audioSystem.PlayPvs(swab.CleanSound, uid);
        args.Handled = true;
    }

    ///<summary>
    /// Swab Applicator, on swab insert check swab has pollen, cancel if it doesn't.
    /// </summary>
    private void OnInsertAttempt(EntityUid uid, BotanySwabComponent swab, ref ContainerGettingInsertedAttemptEvent args)
    {
        //does the container have the botanySwab component (should always be the case)
        if (!HasComp<BotanySwabComponent>(args.Container.Owner))
            return;

        //does the swab have seeddata (aka, is not null)
        if (swab.SeedData != null)
            return;

        //if these are not true, cancel, clean swabs aren't allowed.
        _popupSystem.PopupEntity(Loc.GetString("swab-applicator-needs-pollen"), uid);
        args.Cancel();
        return;
    }
    ///<summary>
    /// Swab Applicator, on swab successfully inserted transfer its SeedData to the applicator
    /// </summary>
    private void OnInsert(EntityUid uid, BotanySwabComponent swab, ref EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<BotanySwabComponent>(args.Container.Owner, out var applicator))
            applicator.SeedData = swab.SeedData;
    }

    ///<summary>
    /// Swab Applicator, on removing swab, set Applicator's SeedData back to null
    /// </summary>
    private void OnRemove(EntityUid uid, BotanySwabComponent swab, ref EntGotRemovedFromContainerMessage args)
    {
        if (TryComp<BotanySwabComponent>(args.Container.Owner, out var applicator))
            applicator.SeedData = null;
    }
}

