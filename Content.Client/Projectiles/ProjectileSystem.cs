using Content.Shared.Projectiles;
using Content.Shared.Spawners.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileComponent, ComponentHandleState>(OnHandleState);
        SubscribeNetworkEvent<ImpactEffectEvent>(OnProjectileImpact);
    }

    private void OnProjectileImpact(ImpactEffectEvent ev)
    {
        if (Deleted(ev.Coordinates.EntityId))
            return;

        var ent = Spawn(ev.Prototype, ev.Coordinates);

        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            sprite[EffectLayers.Unshaded].AutoAnimated = false;
            sprite.LayerMapTryGet(EffectLayers.Unshaded, out var layer);
            var state = sprite.LayerGetState(layer);
            var lifetime = 0.5f;

            if (TryComp<TimedDespawnComponent>(ent, out var despawn))
                lifetime = despawn.Lifetime;

            var anim = new Animation()
            {
                Length = TimeSpan.FromSeconds(lifetime),
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick()
                    {
                        LayerKey = EffectLayers.Unshaded,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(state.Name, 0f),
                        }
                    }
                }
            };

            _player.Play(ent, anim, "impact-effect");
        }
    }

    private void OnHandleState(EntityUid uid, ProjectileComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ProjectileComponentState state) return;
        component.Shooter = state.Shooter;
        component.IgnoreShooter = state.IgnoreShooter;
    }
}
