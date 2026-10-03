using Content.Shared.Inventory;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Base system for accents that should apply both directly and when relayed through other entities.
/// </summary>
public abstract partial class RelayAccentSystem<T> : EntitySystem where T : BaseAccentComponent
{
    [Dependency] private InventorySystem _inventory = default!;

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
        SubscribeLocalEvent<T, StatusEffectRelayedEvent<AccentGetEvent>>(OnStatusEffectRelayAccent, before: RelayAccentBefore, after: RelayAccentAfter);
    }

    protected virtual void OnInventoryRelayAccent(Entity<T> ent, ref InventoryRelayedEvent<AccentGetEvent> args)
    {
        if (!ent.Comp.RelayAccent)
            return;

        // Check owner (character trait accents).
        if (TryComp<T>(args.Owner, out var own) && IsSameAccent((args.Owner, own), ent))
            return;

        // Check items in invontery that apply the same accent.
        var enumerator = _inventory.GetSlotEnumerator(args.Owner, args.Args.TargetSlots);
        while (enumerator.NextItem(out var item))
        {
            if (item == ent.Owner)
                break;

            if (TryComp<T>(item, out var other) && other.RelayAccent && IsSameAccent((item, other), ent))
                return;
        }

        OnAccent(ent, ref args.Args);
    }

    /// <summary>
    /// Check if two accents are the same accent.
    /// </summary>
    protected virtual bool IsSameAccent(Entity<T> a, Entity<T> b)
    {
        return true;
    }

    protected virtual void OnStatusEffectRelayAccent(Entity<T> ent, ref StatusEffectRelayedEvent<AccentGetEvent> args)
    {
        var ev = args.Args;
        OnAccent(ent, ref ev);
        args.Args = ev;
    }

    protected virtual void OnAccent(Entity<T> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, ent);
    }

    /// <summary>
    /// Applies the accent transformation to the provided message.
    /// </summary>
    public virtual string Accentuate(string message, Entity<T>? ent = null)
    {
        return message;
    }
}
