using Content.Shared.Inventory;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Base system for accents that should apply both directly and when relayed through other entities.
/// </summary>
public abstract class RelayAccentSystem<T> : EntitySystem where T : Component
{
    [Dependency] private readonly EntityQuery<RelayAccentsComponent> _relayAccentsQuery = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<T, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<T, InventoryRelayedEvent<AccentGetEvent>>(OnInventoryRelayAccent);
        SubscribeLocalEvent<T, StatusEffectRelayedEvent<AccentGetEvent>>((e, c, ev) =>
        {
            var accentGetEvent = ev.Args;
            OnAccent((e, c), ref accentGetEvent);
        });
    }

    protected virtual void OnInventoryRelayAccent(Entity<T> ent, ref InventoryRelayedEvent<AccentGetEvent> args)
    {
        if (!_relayAccentsQuery.HasComponent(ent))
            return;

        OnAccent(ent, ref args.Args);
    }

    protected virtual void OnAccent(Entity<T> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, ent);
    }

    /// <summary>
    /// Applies the accent transformation to the provided message.
    /// </summary>///
    public virtual string Accentuate(string message, Entity<T>? ent)
    {
        return message;
    }
}
