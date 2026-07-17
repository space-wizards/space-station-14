using Content.Shared.Hands;

namespace Content.Shared.Inventory;

/// <summary>
/// Helper functions for subscribing to component events that are also relayed via hands/inventory.
/// </summary>
public static class RelaySubscriptionHelpers
{
    /// <summary>
    /// Subscribe to an event, along with different relayed event wrappers, in one call.
    /// </summary>
    /// <param name="subs">Subscriptions for the entity system we're subscribing on.</param>
    /// <param name="handler">The event handler to be called for the event.</param>
    /// <param name="baseEvent">Whether to subscribe the base event type.</param>
    /// <param name="inventory">Whether to subscribe for <see cref="T:Content.Shared.Inventory.InventoryRelayedEvent`1"/>.</param>
    /// <param name="held">Whether to subscribe for <see cref="T:Content.Shared.Hands.HeldRelayedEvent`1"/>.</param>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.EntityEventRefHandler{``0,``1},System.Type[],System.Type[])"/>
    public static void SubscribeWithRelay<TComp, TEvent>(
        this EntitySystem.Subscriptions subs,
        EntityEventRefHandler<TComp, TEvent> handler,
        bool baseEvent = true,
        bool inventory = true,
        bool held = true)
        where TEvent : notnull
        where TComp : IComponent
    {
        if (baseEvent)
            subs.SubscribeLocalEvent(handler);

        if (inventory)
        {
            subs.SubscribeLocalEvent((Entity<TComp> ent, ref InventoryRelayedEvent<TEvent> ev) =>
            {
                handler(ent, ref ev.Args);
            });
        }

        if (held)
        {
            subs.SubscribeLocalEvent((Entity<TComp> ent, ref HeldRelayedEvent<TEvent> ev) =>
            {
                handler(ent, ref ev.Args);
            });
        }
    }

    /// <summary>
    /// Subscribe to an event, along with different relayed event wrappers, in one call.
    /// </summary>
    /// <param name="subs">Subscriptions for the entity system we're subscribing on.</param>
    /// <param name="handler">The event handler to be called for the event.</param>
    /// <param name="baseEvent">Whether to subscribe the base event type.</param>
    /// <param name="inventory">Whether to subscribe for <see cref="T:Content.Shared.Inventory.InventoryRelayedEvent`1"/>.</param>
    /// <param name="held">Whether to subscribe for <see cref="T:Content.Shared.Hands.HeldRelayedEvent`1"/>.</param>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.ComponentEventHandler{``0,``1},System.Type[],System.Type[])"/>
    public static void SubscribeWithRelay<TComp, TEvent>(
        this EntitySystem.Subscriptions subs,
        ComponentEventHandler<TComp, TEvent> handler,
        bool baseEvent = true,
        bool inventory = true,
        bool held = true)
        where TEvent : notnull
        where TComp : IComponent
    {
        if (baseEvent)
            subs.SubscribeLocalEvent(handler);

        if (inventory)
        {
            subs.SubscribeLocalEvent((EntityUid uid, TComp component, InventoryRelayedEvent<TEvent> args) =>
            {
                handler(uid, component, args.Args);
            });
        }

        if (held)
        {
            subs.SubscribeLocalEvent((EntityUid uid, TComp component, HeldRelayedEvent<TEvent> args) =>
            {
                handler(uid, component, args.Args);
            });
        }
    }

    /// <summary>
    /// Subscribe to an event, along with different relayed event wrappers, in one call.
    /// </summary>
    /// <param name="subs">Subscriptions for the entity system we're subscribing on.</param>
    /// <param name="handler">The event handler to be called for the event.</param>
    /// <param name="baseEvent">Whether to subscribe the base event type.</param>
    /// <param name="inventory">Whether to subscribe for <see cref="T:Content.Shared.Inventory.InventoryRelayedEvent`1"/>.</param>
    /// <param name="held">Whether to subscribe for <see cref="T:Content.Shared.Hands.HeldRelayedEvent`1"/>.</param>
    /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.ComponentEventRefHandler{``0,``1},System.Type[],System.Type[])"/>
    public static void SubscribeWithRelay<TComp, TEvent>(
        this EntitySystem.Subscriptions subs,
        ComponentEventRefHandler<TComp, TEvent> handler,
        bool baseEvent = true,
        bool inventory = true,
        bool held = true)
        where TEvent : notnull
        where TComp : IComponent
    {
        if (baseEvent)
            subs.SubscribeLocalEvent(handler);

        if (inventory)
        {
            subs.SubscribeLocalEvent((EntityUid uid, TComp component, ref InventoryRelayedEvent<TEvent> args) =>
            {
                handler(uid, component, ref args.Args);
            });
        }

        if (held)
        {
            subs.SubscribeLocalEvent((EntityUid uid, TComp component, ref HeldRelayedEvent<TEvent> args) =>
            {
                handler(uid, component, ref args.Args);
            });
        }
    }
}
