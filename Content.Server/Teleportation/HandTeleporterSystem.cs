using System.Threading;
using Content.Server.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Teleportation;

/// <summary>
/// This handles creating portals from a hand teleporter.
/// </summary>
public sealed class HandTeleporterSystem : EntitySystem
{
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doafter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HandTeleporterComponent, UseInHandEvent>(OnUseInHand);

        SubscribeLocalEvent<HandTeleporterComponent, HandTeleporterSuccessEvent>(OnPortalSuccess);
        SubscribeLocalEvent<HandTeleporterComponent, HandTeleporterCancelledEvent>(OnPortalCancelled);
    }

    private void OnPortalSuccess(EntityUid uid, HandTeleporterComponent component, HandTeleporterSuccessEvent args)
    {
        component.CancelToken = null;
        HandlePortalUpdating(uid, component, args.User);
    }

    private void OnPortalCancelled(EntityUid uid, HandTeleporterComponent component, HandTeleporterCancelledEvent args)
    {
        component.CancelToken = null;
    }

    private void OnUseInHand(EntityUid uid, HandTeleporterComponent component, UseInHandEvent args)
    {
        if (Deleted(component.FirstPortal))
            component.FirstPortal = null;

        if (Deleted(component.SecondPortal))
            component.SecondPortal = null;

        if (component.CancelToken != null)
        {
            component.CancelToken.Cancel();
            return;
        }

        if (component.FirstPortal != null && component.SecondPortal != null)
        {
            // handle removing portals immediately as opposed to a doafter
            HandlePortalUpdating(uid, component, args.User);
        }
        else
        {
            var xform = Transform(args.User);
            if (xform.ParentUid != xform.GridUid)
                return;

            component.CancelToken = new CancellationTokenSource();
            var doafterArgs = new DoAfterEventArgs(args.User, component.PortalCreationDelay,
                component.CancelToken.Token, used: uid)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnUserMove = true,
                MovementThreshold = 0.5f,
                UsedCancelledEvent = new HandTeleporterCancelledEvent(),
                UsedFinishedEvent = new HandTeleporterSuccessEvent(args.User)
            };

            _doafter.DoAfter(doafterArgs);
        }
    }


    /// <summary>
    ///     Creates or removes a portal given the state of the hand teleporter.
    /// </summary>
    private void HandlePortalUpdating(EntityUid uid, HandTeleporterComponent component, EntityUid user)
    {
        if (Deleted(user))
            return;

        var xform = Transform(user);

        // Create the first portal.
        if (component.FirstPortal == null && component.SecondPortal == null)
        {
            // don't portal
            if (xform.ParentUid != xform.GridUid)
                return;

            var timeout = EnsureComp<PortalTimeoutComponent>(user);
            timeout.EnteredPortal = null;
            component.FirstPortal = Spawn(component.FirstPortalPrototype, Transform(user).Coordinates);
            _audio.PlayPvs(component.NewPortalSound, uid);
        }
        else if (component.SecondPortal == null)
        {
            var timeout = EnsureComp<PortalTimeoutComponent>(user);
            timeout.EnteredPortal = null;
            component.SecondPortal = Spawn(component.SecondPortalPrototype, Transform(user).Coordinates);
            _link.TryLink(component.FirstPortal!.Value, component.SecondPortal.Value, true);
            _audio.PlayPvs(component.NewPortalSound, uid);
        }
        else
        {
            // Clear both portals
            QueueDel(component.FirstPortal!.Value);
            QueueDel(component.SecondPortal!.Value);

            component.FirstPortal = null;
            component.SecondPortal = null;
            _audio.PlayPvs(component.ClearPortalsSound, uid);
        }
    }
}
