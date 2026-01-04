using Content.Server.Botany.Components;
using Content.Server.Botany.Events;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Swab;

namespace Content.Server.Botany.Systems;

public sealed class BotanySwabSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly MutationSystem _mutation = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

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
    private void OnExamined(Entity<BotanySwabComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.PlantData != null)
            args.PushMarkup(Loc.GetString("swab-used"));
        else
            args.PushMarkup(Loc.GetString("swab-unused"));
    }

    /// <summary>
    /// Handles swabbing a plant.
    /// </summary>
    private void OnAfterInteract(Entity<BotanySwabComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantComponent>(args.Target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.SwabDelay, new BotanySwabDoAfterEvent(), ent.Owner, target: args.Target, used: ent.Owner)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// Save seed data or cross-pollenate.
    /// </summary>
    private void OnDoAfter(Entity<BotanySwabComponent> ent, ref BotanySwabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !HasComp<PlantComponent>(args.Args.Target))
            return;

        var targetPlant = args.Args.Target.Value;

        if (ent.Comp.PlantData == null)
        {
            // Pick up pollen snapshot.
            ent.Comp.PlantProtoId = MetaData(targetPlant).EntityPrototype?.ID;
            ent.Comp.PlantData = _botany.ClonePlantSnapshotData(targetPlant);

            _popup.PopupEntity(Loc.GetString("botany-swab-from"), targetPlant, args.Args.User);
        }
        else
        {
            _mutation.CrossMutations(ent.Comp.PlantData, ent.Comp.PlantProtoId, targetPlant);

            // Notify growth systems to apply their per-component cross logic.
            var crossEv = new PlantCrossPollinateEvent(ent.Comp.PlantData, ent.Comp.PlantProtoId);
            RaiseLocalEvent(targetPlant, ref crossEv);

            // Swap: store old target pollen on the swab, apply cross to the target using swab pollen.
            ent.Comp.PlantProtoId = MetaData(targetPlant).EntityPrototype?.ID;
            ent.Comp.PlantData = _botany.ClonePlantSnapshotData(targetPlant);

            _popup.PopupEntity(Loc.GetString("botany-swab-to"), targetPlant, args.Args.User);
        }

        args.Handled = true;
    }
}

