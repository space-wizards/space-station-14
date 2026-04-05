using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Photography;

/// <summary>
/// Handles everything related to photography.
/// </summary>
public sealed class PhotographySystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityTableSystem _tables = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotographComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PictureTakerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PictureTakerComponent, BeforeRangedInteractEvent>(OnRangedInteract);
        SubscribeLocalEvent<PictureTakerComponent, MeleeHitEvent>(OnCameraMeleeHit);
    }

    private void OnExamined(Entity<PhotographComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(PhotographComponent)))
        {
            if (string.IsNullOrEmpty(ent.Comp.NameText))
                args.PushText(Loc.GetString("photograph-name-text-empty"));
            else
                args.PushText(ent.Comp.NameText);
            if (ent.Comp.Description != null)
                // TODO: For some weird reason ExamineSystem is adding a new line at the end of message we are pushing with each examine.
                // I'm not soaping this PR even more, so for now I'll just bandaid that by sending a clone to prevent it from getting modified.
                args.PushMessage(new FormattedMessage(ent.Comp.Description));
        }
    }

    private void OnUseInHand(Entity<PictureTakerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !TryComp<FlashComponent>(ent.Owner, out var flashComp) || !_flash.TryUseFlashItem((ent.Owner, flashComp), args.User))
            return;

        // If not aimed at anything just create a flash and photograph without any specific target.
        TakePicture(ent, null, args.User);
        _flash.FlashArea(ent.Owner, args.User, flashComp.Range, flashComp.AoeFlashDuration, flashComp.SlowTo, true, flashComp.Probability);
        args.Handled = true;
    }

    /// <summary>
    /// Processes the entity hit by a camera and prints a picture of them.
    /// </summary>
    private void OnCameraMeleeHit(Entity<PictureTakerComponent> ent, ref MeleeHitEvent args)
    {
        // if we hit nothing, there's nothing to take a picture of.
        if (!args.HitEntities.Any())
            return;
        if (TryComp<FlashComponent>(ent.Owner, out var flashComp))
        {
            // we also need to be able to flash (this also checks for charges)
            if (!_flash.TryUseFlashItem((ent.Owner, flashComp), args.User))
                return;
        }
        else
        {
            // we need a flash to take a picture!
            return;
        }
        foreach (var entity in args.HitEntities)
        {
            // produces the picture so our camera works
            TakePicture(ent, entity, args.User);
            // TODO: Currently this just hijacks the FlashOnMelee property of FlashComponent and uses the flash on melee even if its set to false. A better implementation would interface better with the flashing system (maybe the check could be on that system?)
            // flashes the entity.
            var stunDuration = TimeSpan.Zero;
            if (flashComp.MeleeStunDuration != null)
                stunDuration=flashComp.MeleeStunDuration.Value;
            _flash.Flash(entity, args.User, ent.Owner, stunDuration, flashComp.SlowTo, melee:true, displayPopup:true);
            // we can only take one picture of one entity per swing!
            break;
        }

    }

    // TODO: This or most of the other systems subscribing to BeforeRangedInteractEvent shouldn't be using this event as handling it stops contact interaction with the used tool,
    // but this will need some cleanup of how SharedInteractionSystem handles the code flow. Also the event is raised for both in-range and out of range interactions, which
    // is what the subscribers are using it for, but does not seem originally intended from the naming convention.
    private void OnRangedInteract(Entity<PictureTakerComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !TryComp<FlashComponent>(ent.Owner, out var flashComp) || !_flash.TryUseFlashItem((ent.Owner, flashComp), args.User))
            return;
        TakePicture(ent, args.Target, args.User);
        _flash.FlashArea(ent.Owner, args.User, flashComp.Range, flashComp.AoeFlashDuration, flashComp.SlowTo, true, flashComp.Probability);
        args.Handled = true;
    }

    /// <summary>
    /// Processes entity aimed at with a camera and prints a picture of it.
    /// TODO: This is basically a placeholder mechanic for a more elaborate photography system.
    /// However, this will need major refactoring to be possible. See
    /// https://github.com/space-wizards/docs/pull/307 and
    /// https://github.com/space-wizards/space-station-14/pull/43327
    /// for details.
    /// </summary>
    public void TakePicture(Entity<PictureTakerComponent> camera, EntityUid? target, EntityUid user)
    {
        if (_net.IsClient)
            return; // Can't interact with predictively spawned entities yet.

        var tableResult = _tables.GetSpawns(camera.Comp.Photographs);
        var coords = Transform(user).Coordinates;

        FormattedMessage? description = null;
        string? nameText = null;
        if (target != null)
        {
            description = _examine.GetExamineText(target.Value, user);
            // Get the full string now instead of indexing it later because we need the entity to know if it uses a proper noun or not.
            nameText = Loc.GetString("photograph-name-text", ("entity", Identity.Entity(target.Value, EntityManager)));
            // We don't want photographs to contain the descriptions of other photographs, because that makes entities with, in theory, infinite descriptions.
            if (HasComp<PhotographComponent>(target.Value))
            {
                description = null;
                nameText = Loc.GetString("photograph-name-text-photograph");
            }
        }

        foreach (var prototype in tableResult)
        {
            // we generate an individual photograph (there should be only one tough)
            var spawned = Spawn(prototype, coords);
            var photoComp = EnsureComp<PhotographComponent>(spawned);
            photoComp.NameText = nameText;
            photoComp.Description = description;
            Dirty(spawned, photoComp);

            _hands.PickupOrDrop(user, spawned, dropNear: true);
        }
    }
}
