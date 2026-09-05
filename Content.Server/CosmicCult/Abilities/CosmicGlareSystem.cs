using System.Linq;
using Content.Server.Flash;
using Content.Server.Stunnable;
using Content.Shared.CosmicCult;
using Content.Shared.CosmicCult.Components;
using Content.Shared.CosmicCult.Components.Actions;
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

public sealed partial class CosmicGlareSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private FlashSystem _flash = default!;
    [Dependency] private SharedPoweredLightSystem _poweredLight = default!;
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private CosmicCultSystem _cosmicCult = default!;
    [Dependency] private SharedInteractionSystem _interact = default!;

    private HashSet<Entity<PoweredLightComponent>> _lights = [];

    [SubscribeLocalEvent]
    private void OnCosmicGlare(Entity<CosmicActionGlareComponent> ent, ref EventCosmicGlare args)
    {
        if (!TryComp<CosmicCultActionComponent>(ent, out var action))
            return;

        _audio.PlayPvs(action.Sfx, ent);
        Spawn(action.Vfx, Transform(ent).Coordinates);
        args.Handled = true;

        var stun = action.Empowered ? ent.Comp.StunEmpowered : ent.Comp.StunDefault;
        var range = action.Empowered ? ent.Comp.RangeDefault : ent.Comp.RangeEmpowered;
        var duration = action.Empowered ? ent.Comp.DurationDefault : ent.Comp.DurationEmpowered;
        var penalty = action.Empowered ? ent.Comp.MovePenaltyDefault : ent.Comp.MovePenaltyEmpowered;

        _lights.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, range, _lights);

        foreach (var entity in _lights)
        {
            _poweredLight.TryDestroyBulb(entity);
        }

        var targetFilter = Filter.Pvs(ent).RemoveWhere(player =>
        {
            if (player.AttachedEntity == null)
                return true;

            var ent = player.AttachedEntity.Value;
            if (!HasComp<MobStateComponent>(ent) || _cosmicCult.EntityIsCultist(ent))
                return true;

            return !_interact.InRangeUnobstructed((ent, Transform(ent)), (ent, Transform(ent)), range: 0, collisionMask: CollisionGroup.Impassable);
        });

        var targets = new HashSet<NetEntity>(targetFilter.RemovePlayerByAttachedEntity(ent).Recipients.Select(ply => GetNetEntity(ply.AttachedEntity!.Value)));
        foreach (var target in targets)
        {
            var targetEnt = GetEntity(target);

            _flash.Flash(targetEnt, ent, args.Action, duration, penalty, false, false, stun);

            if (HasComp<BorgChassisComponent>(targetEnt))
                _stun.TryAddParalyzeDuration(targetEnt, duration / 2);

            _color.RaiseEffect(Color.CadetBlue, new List<EntityUid>() { targetEnt }, Filter.Pvs(targetEnt, entityManager: EntityManager));
        }
    }
}
