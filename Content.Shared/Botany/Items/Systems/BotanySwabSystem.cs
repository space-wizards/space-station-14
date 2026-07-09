using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Events;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Swab;

namespace Content.Shared.Botany.Items.Systems;

public sealed partial class BotanySwabSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <summary>
    /// This handles swab examination text
    /// so you can tell if they are used or not.
    /// </summary>
    [SubscribeLocalEvent]
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
    [SubscribeLocalEvent]
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
    [SubscribeLocalEvent]
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

            _popup.PopupClient(Loc.GetString("botany-swab-from"), targetPlant, args.Args.User);
        }
        else
        {
            var pollenData = ent.Comp.PlantData.Value;
            _mutation.CrossMutations(pollenData, ent.Comp.PlantProtoId, targetPlant);

            // Notify growth systems to apply their per-component cross logic.
            var crossEv = new PlantCrossPollinateEvent(pollenData, ent.Comp.PlantProtoId);
            RaiseLocalEvent(targetPlant, ref crossEv);

            // Swap: store old target pollen on the swab, apply cross to the target using swab pollen.
            ent.Comp.PlantProtoId = MetaData(targetPlant).EntityPrototype?.ID;
            ent.Comp.PlantData = _botany.ClonePlantSnapshotData(targetPlant);

            _popup.PopupClient(Loc.GetString("botany-swab-to"), targetPlant, args.Args.User);
        }

        Dirty(ent);
        args.Handled = true;
    }
}
