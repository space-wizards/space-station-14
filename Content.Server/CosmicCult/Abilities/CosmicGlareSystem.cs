using System.Linq;
using Content.Server.Flash;
using Content.Server.Stunnable;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Effects;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.CosmicCult.Abilities;

public sealed class CosmicGlareSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private FlashSystem _flash = default!;
    [Dependency] private SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedCosmicCultSystem _cosmicCult = default!;
    [Dependency] private SharedInteractionSystem _interact = default!;

    private HashSet<Entity<PoweredLightComponent>> _lights = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicCultistComponent, EventCosmicGlare>(OnCosmicGlare);
    }

    private void OnCosmicGlare(Entity<CosmicCultistComponent> uid, ref EventCosmicGlare args)
    {
        _audio.PlayPvs(uid.Comp.GlareSfx, uid);
        Spawn(uid.Comp.GlareVfx, Transform(uid).Coordinates);
        args.Handled = true;

        _lights.Clear();
        _lookup.GetEntitiesInRange<PoweredLightComponent>(Transform(uid).Coordinates, uid.Comp.CosmicGlareRange, _lights);

        foreach (var entity in _lights)
            _poweredLight.TryDestroyBulb(entity);

        var targetFilter = Filter.Pvs(uid).RemoveWhere(player =>
        {
            if (player.AttachedEntity == null)
                return true;

            var ent = player.AttachedEntity.Value;
            if (!HasComp<MobStateComponent>(ent) || _cosmicCult.EntityIsCultist(ent))
                return true;

            return !_interact.InRangeUnobstructed((uid, Transform(uid)), (ent, Transform(ent)), range: 0, collisionMask: CollisionGroup.Impassable);
        });

        var targets = new HashSet<NetEntity>(targetFilter.RemovePlayerByAttachedEntity(uid).Recipients.Select(ply => GetNetEntity(ply.AttachedEntity!.Value)));
        foreach (var target in targets)
        {
            var targetEnt = GetEntity(target);

            _flash.Flash(targetEnt, uid, args.Action, uid.Comp.CosmicGlareDuration, uid.Comp.CosmicGlarePenalty, false, false, uid.Comp.CosmicGlareStun);

            if (HasComp<BorgChassisComponent>(targetEnt))
            {
                _stun.TryAddParalyzeDuration(targetEnt, uid.Comp.CosmicGlareDuration / 2);
            }

            _color.RaiseEffect(Color.CadetBlue, new List<EntityUid>() { targetEnt }, Filter.Pvs(targetEnt, entityManager: EntityManager));
        }
    }
}
