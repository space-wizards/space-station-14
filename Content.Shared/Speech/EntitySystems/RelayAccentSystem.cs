using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Speech.EntitySystems;

/// <summary>
/// Base system for accents that should apply both directly and when relayed through other entities.
/// </summary>
public abstract class RelayAccentSystem<T> : EntitySystem where T : Component
{
    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<T, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<T, StatusEffectRelayedEvent<AccentGetEvent>>(OnAccentRelayed);
    }

    /// <summary>
    /// Applies the accent transformation to the provided message.
    /// </summary>
    private string Accentuate(EntityUid uid, T comp, string message)
    {
        return AccentuateInternal(uid, comp, message);
    }

    protected abstract string AccentuateInternal(EntityUid uid, T comp, string message);

    private void OnAccent(Entity<T> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Entity, ent.Comp, args.Message);
    }

    private void OnAccentRelayed(Entity<T> ent, ref StatusEffectRelayedEvent<AccentGetEvent> args)
    {
        args.Args.Message = Accentuate(args.Args.Entity, ent.Comp, args.Args.Message);
    }
}
