using Content.Server.GameTicking;
using Content.Server.Destructible;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Robust.Shared.Map;

namespace Content.Server.SpaceArena;

public sealed partial class SpaceArenaHubProtectionSystem : EntitySystem
{
    private readonly HashSet<MapId> _protectedMaps = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<MapRemovedEvent>(OnMapRemoved);
        SubscribeLocalEvent<DamageableComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<DestructibleComponent, DestructionAttemptEvent>(OnDestructionAttempt);
    }

    private void OnPostGameMapLoad(PostGameMapLoad args)
    {
        if (!args.GameMap.SpaceArenaHubProtection)
            return;

        _protectedMaps.Add(args.Map);
    }

    private void OnMapRemoved(MapRemovedEvent args)
    {
        _protectedMaps.Remove(args.MapId);
    }

    private void OnBeforeDamageChanged(
        Entity<DamageableComponent> entity,
        ref BeforeDamageChangedEvent args)
    {
        if (_protectedMaps.Contains(Transform(entity).MapID))
            args.Cancelled = true;
    }

    private void OnDestructionAttempt(
        Entity<DestructibleComponent> entity,
        ref DestructionAttemptEvent args)
    {
        if (_protectedMaps.Contains(Transform(entity).MapID))
            args.Cancel();
    }
}
