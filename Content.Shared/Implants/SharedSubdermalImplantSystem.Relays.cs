using Content.Shared.Chat;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Store;

namespace Content.Shared.Implants;

public abstract partial class SharedSubdermalImplantSystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, SuicideEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, TransformSpeakerNameEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, TransformSpeechEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, SeeIdentityAttemptEvent>(RelayToImplantEvent);

        // Ref relays, for when you need to write to the event!
        SubscribeLocalEvent<ImplantedComponent, CurrencyInsertAttemptEvent>(RefRelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, GetStoreEvent>(RefRelayToImplantEvent);
    }

    /// <summary>
    /// Relays events from the implanted to the implant.
    /// </summary>
    private void RelayToImplantEvent<T>(EntityUid uid, ImplantedComponent component, T args) where T : notnull
    {
        if (!_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        var relayEv = new ImplantRelayEvent<T>(args, uid);
        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (args is HandledEntityEventArgs { Handled: true })
                return;

            RaiseLocalEvent(implant, relayEv);
        }
    }

    /// <summary>
    /// Relays events from the implanted to the implant.
    /// </summary>
    private void RefRelayToImplantEvent<T>(Entity<ImplantedComponent> entity, ref T args) where T : notnull
    {
        if (!_container.TryGetContainer(entity, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        var relayEv = new ImplantRelayEvent<T>(args, entity);
        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (args is HandledEntityEventArgs { Handled: true })
                return;

            RaiseLocalEvent(implant, relayEv);
        }

        args = relayEv.Event;
    }
}

/// <summary>
/// Wrapper for relaying events from an implanted entity to their implants.
/// </summary>
public sealed class ImplantRelayEvent<T> where T : notnull
{
    public T Event;

    public readonly EntityUid ImplantedEntity;

    public ImplantRelayEvent(T ev, EntityUid implantedEntity)
    {
        Event = ev;
        ImplantedEntity = implantedEntity;
    }
}
