using Content.Server.Antag;
using Content.Server.Traits;
using Content.Shared.GameTicking;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Systems;

namespace Content.Server.Trigger.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class TriggerOnPlayerSpawnCompleteSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TriggerOnPlayerSpawnCompleteComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        UpdatesAfter.Add(typeof(AntagSelectionSystem));
        UpdatesAfter.Add(typeof(TraitSystem));
    }

    private void OnPlayerSpawn(Entity<TriggerOnPlayerSpawnCompleteComponent> ent, ref PlayerSpawnCompleteEvent args)
    {
        _trigger.Trigger(ent.Owner, null, ent.Comp.KeyOut);
    }
}
