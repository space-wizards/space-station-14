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

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HandTeleporterComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, HandTeleporterComponent component, UseInHandEvent args)
    {
        if (Deleted(component.FirstPortal))
            component.FirstPortal = null;

        if (Deleted(component.SecondPortal))
            component.SecondPortal = null;

        // Create the first portal.
        if (component.FirstPortal == null && component.SecondPortal == null)
        {
            var timeout = EnsureComp<PortalTimeoutComponent>(args.User);
            timeout.EnteredPortal = null;
            component.FirstPortal = Spawn(component.FirstPortalPrototype, Transform(args.User).Coordinates);
            _audio.PlayPvs(component.NewPortalSound, uid);
        }
        else if (component.SecondPortal == null)
        {
            var timeout = EnsureComp<PortalTimeoutComponent>(args.User);
            timeout.EnteredPortal = null;
            component.SecondPortal = Spawn(component.SecondPortalPrototype, Transform(args.User).Coordinates);
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
