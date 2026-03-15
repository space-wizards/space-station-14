using Content.Shared.Inventory;
using Content.Shared.Speech;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Speech.EntitySystems;

/// <summary>
/// Base system for accents. Handles subscriptions and relay events.
/// </summary>
/// <typeparam name="T">Accent component.</typeparam>
public abstract class BaseAccentSystem<T> : EntitySystem
    where T : Component
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, AccentGetEvent>(OnAccent);
        SubscribeLocalEvent<T, InventoryRelayedEvent<AccentGetEvent>>((e, c, ev) => OnAccent((e, c), ref ev.Args));
        SubscribeLocalEvent<T, StatusEffectRelayedEvent<AccentGetEvent>>((e, c, ev) =>
        {
            var accentGetEvent = ev.Args;
            OnAccent((e, c), ref accentGetEvent);
        });
    }

    protected virtual void OnAccent(Entity<T> ent, ref AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, ent);
    }

    /// <summary>
    /// Accents a message based on the component.
    /// </summary>
    public virtual string Accentuate(string message, Entity<T>? ent)
    {
        return message;
    }
}
