using Content.Shared.Inventory;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Base system for accents that should apply both directly and when relayed through other entities.
/// </summary>
public abstract class RelayAccentSystem<T> : EntitySystem where T : BaseAccentComponent
{
    /// <summary>
    /// Systems this accent should run before for direct speech accenting.
    /// </summary>
    protected virtual Type[]? AccentBefore => null;

    /// <summary>
    /// Systems this accent should run after for direct speech accenting.
    /// </summary>
    protected virtual Type[]? AccentAfter => null;

    /// <summary>
    /// Systems this accent should run before for relayed speech accenting.
    /// </summary>
    protected virtual Type[]? RelayAccentBefore => AccentBefore;

    /// <summary>
    /// Systems this accent should run after for relayed speech accenting.
    /// </summary>
    protected virtual Type[]? RelayAccentAfter => AccentAfter;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<T, AccentGetEvent>(OnAccent, before: AccentBefore, after: AccentAfter);
        SubscribeLocalEvent<T, InventoryRelayedEvent<AccentGetEvent>>(OnInventoryRelayAccent, before: RelayAccentBefore, after: RelayAccentAfter);
        SubscribeLocalEvent<T, StatusEffectRelayedEvent<AccentGetEvent>>((e, c, ev) =>
        {
            var accentGetEvent = ev.Args;
            OnAccent((e, c), ref accentGetEvent);
        },
        before: RelayAccentBefore,
        after: RelayAccentAfter);
    }

    protected virtual void OnInventoryRelayAccent(Entity<T> ent, ref InventoryRelayedEvent<AccentGetEvent> args)
    {
        if (!ent.Comp.RelayAccent)
            return;

        OnAccent(ent, ref args.Args);
    }

    protected virtual void OnAccent(Entity<T> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, ent);
    }

    public virtual string Accentuate(string message)
    {
        return message;
    }

    /// <summary>
    /// Applies the accent transformation to the provided message.
    /// </summary>
    public virtual string Accentuate(string message, Entity<T>? ent)
    {
        return Accentuate(message);
    }
}
