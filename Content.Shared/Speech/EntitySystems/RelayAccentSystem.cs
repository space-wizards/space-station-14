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

        // Prevent the same accent from applying twice.
        if (HasAccent(args.Owner, ent))
            return;

        OnAccent(ent, ref args.Args);
    }

    /// <summary>
    /// Checks if target already has this accent.
    /// </summary>
    public bool HasAccent(EntityUid target, Entity<T> accentEnt)
    {
        // If not ReplacementAccent all accentProto checks are null == null (true).
        var accentProto = (accentEnt.Comp as ReplacementAccentComponent)?.Accent;

        // Check owner (character trait accents).
        if (TryComp<T>(target, out var own) && (own as ReplacementAccentComponent)?.Accent == accentProto)
            return true;

        // Check items in invontery that apply the same accent.
        var enumerator = _inventory.GetSlotEnumerator(target, SlotFlags.WITHOUT_POCKET);
        while (enumerator.NextItem(out var item))
        {
            if (item == accentEnt.Owner)
                // Runs for every relayAccent item. A continue here would make neither apply.
                break;

            if (TryComp<T>(item, out var other) && other.RelayAccent
                && (other as ReplacementAccentComponent)?.Accent == accentProto)
                return true;
        }

        return false;
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
