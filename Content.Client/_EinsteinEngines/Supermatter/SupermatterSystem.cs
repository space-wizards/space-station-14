using Content.Shared._EinsteinEngines.Supermatter.Components;
using Content.Shared._EinsteinEngines.Supermatter.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._EinsteinEngines.Supermatter;

public sealed partial class SupermatterSystem : SharedSupermatterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SupermatterSpriteUpdateEvent>(OnSpriteUpdated);
    }

    private void OnSpriteUpdated(SupermatterSpriteUpdateEvent args)
    {
        var uid = EntityManager.GetEntity(args.Entity);

        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerSetState(0, args.State);
        }
    }
}
