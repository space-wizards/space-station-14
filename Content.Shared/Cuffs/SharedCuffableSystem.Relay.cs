using Content.Shared.Chat;
using Content.Shared.Cuffs.Components;
using Content.Shared.Speech;

namespace Content.Shared.Cuffs;

/// <summary>
/// Handles relaying events from a handcuffed entity to its handcuffs.
/// </summary>
public abstract partial class SharedCuffableSystem
{
    protected void InitializeRelay()
    {
        SubscribeLocalEvent<CuffableComponent, AccentGetEvent>(RefRelayCuffedEvent);
        SubscribeLocalEvent<CuffableComponent, BeforeEmoteEvent>(RefRelayCuffedEvent);
    }

    protected void RefRelayCuffedEvent<T>(Entity<CuffableComponent> ent, ref T args)
    {
        RelayCuffedEvent(ent, ref args);
    }

    public void RelayCuffedEvent<T>(Entity<CuffableComponent> ent, ref T args)
    {
        if (!IsCuffed(ent.AsNullable(), false))
            return;

        // This copies the by-ref event if it is a struct
        var ev = new CuffedRelayEvent<T>(args, ent.Owner);
        foreach (var cuffsEnt in GetAllCuffs((ent, ent)))
        {
            RaiseLocalEvent(cuffsEnt, ev);
        }

        // ...and now we copy it back.
        args = ev.Args;
    }
}

/// <summary>
/// Event wrapper for relayed events. Relays to any handcuffs that are currently applied to
/// a handcuffed entity. Useful for checking/applying specific behaviors
/// unique to the given handcuffs when handcuffed.
/// </summary>
/// <remarks>
/// Handcuffs are not actually in the user's hands or inventory; they are stored in a separate container on the entity,
/// with virtual blocker objects taking up the user's hands. Hence the need for this dedicated relay event.
/// </remarks>
public sealed class CuffedRelayEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;

    public EntityUid Cuffed;

    public CuffedRelayEvent(TEvent args, EntityUid owner)
    {
        Args = args;
        Cuffed = owner;
    }
}
