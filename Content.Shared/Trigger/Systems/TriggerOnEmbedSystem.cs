using Content.Shared.Projectiles;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Whitelist;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// This handles <see cref="TriggerOnEmbedComponent"/> subscriptions.
/// </summary>
public sealed class TriggerOnEmbedSystem : TriggerOnXSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerOnEmbedComponent, EmbedEvent>(OnEmbed);
        SubscribeLocalEvent<TriggerOnUnembedComponent, StopEmbedEvent>(OnStopEmbed);
    }

    private void OnEmbed(Entity<TriggerOnEmbedComponent> ent, ref EmbedEvent args)
    {
        var user = ent.Comp.UserIsEmbed ? args.Embedded : args.Shooter;
        Trigger.Trigger(ent, user, ent.Comp.KeyOut);
    }

    private void OnStopEmbed(Entity<TriggerOnUnembedComponent> ent, ref StopEmbedEvent args)
    {
        var user = ent.Comp.UserIsEmbed ? args.Embedded : args.Detacher;
        Trigger.Trigger(ent, user, ent.Comp.KeyOut);
    }
}
