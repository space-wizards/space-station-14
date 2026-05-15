using Content.Shared.Clothing.Components;
using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class FleetingClothingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FleetingClothingComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<FleetingClothingComponent, BeforeGettingUnequippedEvent>(OnBeforeGettingUnequipped);
        SubscribeLocalEvent<FleetingClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnExamine(Entity<FleetingClothingComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        string? examineText = null;
        // check if the item is clothing, is currently equipped and if the wearer is the examiner
        if (_clothing.IsEquipped(ent.Owner) && Transform(ent.Owner).ParentUid == args.Examiner)
        {
            // text to show to the wearer only
            if (ent.Comp.ExamineWearer != null)
                examineText = Loc.GetString(ent.Comp.ExamineWearer);
        }
        else
        {
            // text to show to everyone else
            if (ent.Comp.ExamineOthers != null)
                examineText = Loc.GetString(ent.Comp.ExamineOthers);
        }

        if (!string.IsNullOrEmpty(examineText))
            args.PushMarkup(examineText);
    }

    // Raised before the item is being unequipped.
    // We have to use QueueDel instead of Del because directly deleting the entity while an event is being raised on it will cause errors.
    // We can't do this in.GotUnequippedEvent because container events don't include the user.
    private void OnBeforeGettingUnequipped(Entity<FleetingClothingComponent> ent, ref BeforeGettingUnequippedEvent args)
    {
        if (ent.Comp.DestroyOnUnequip)
            _destructibleSystem.DestroyEntity(ent.Owner); // Empty containers first.
        else
            PredictedQueueDel(ent.Owner); // Poof!

        // Use coords because the entity will be deleted.
        var coords = Transform(ent).Coordinates;
        if (ent.Comp.PlaySoundOnSelfUnequip || args.User != args.EquipTarget)
            _audio.PlayPredicted(ent.Comp.RemovedSound, coords, args.User);

        if (args.User == args.EquipTarget)
        {
            // If the wearer removes the item themselves show specific messages to them and others.
            string? selfMessage = null;
            string? othersMessage = null;
            if (ent.Comp.SelfUnquipPopupWearer != null)
                selfMessage = Loc.GetString(ent.Comp.SelfUnquipPopupWearer, ("item", ent.Owner));
            if (ent.Comp.SelfUnquipPopupOthers != null)
                othersMessage = Loc.GetString(ent.Comp.SelfUnquipPopupOthers, ("item", ent.Owner));

            // Use the wearer for the popup location because the item item itself will get deleted.
            _popup.PopupPredicted(selfMessage, othersMessage, args.EquipTarget, args.User, PopupType.LargeCaution);
        }
        else
        {
            // Show the same popup message for everyone.
            if (ent.Comp.RemovedPopup != null)
                _popup.PopupPredicted(Loc.GetString(ent.Comp.RemovedPopup, ("item", ent.Owner)), args.EquipTarget, args.User, PopupType.LargeCaution);
        }
    }

    // In case the item was somehow removed by any other means not using TryUnequip.
    private void OnGotUnequipped(Entity<FleetingClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return; // The results of the container change were already networked as part of the same game state.

        if (Terminating(ent.Owner) || EntityManager.IsQueuedForDeletion(ent.Owner))
            return; // Don't do the popups or sound twice.

        if (Terminating(args.EquipTarget))
            return; // Don't do anything if the item is removed due to the wearer getting deleted.

        if (ent.Comp.DestroyOnUnequip)
            _destructibleSystem.DestroyEntity(ent.Owner); // Empty containers first.
        else
            PredictedQueueDel(ent.Owner); // Poof!

        // Can't predict the popup or sound without a user.
        // TODO: Make the popup and sound API sane and remove this guard.
        if (_net.IsClient)
            return;

        // Use the wearer for the popup location because the item item itself will get deleted.
        _audio.PlayPvs(ent.Comp.RemovedSound, args.EquipTarget);
        if (ent.Comp.RemovedPopup != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.RemovedPopup, ("item", ent.Owner)), args.EquipTarget, PopupType.LargeCaution);
    }
}
