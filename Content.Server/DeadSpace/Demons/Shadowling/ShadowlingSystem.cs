// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Robust.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.DeadSpace.Demons.Shadowling;
using Content.Shared.Examine;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage.Systems;

namespace Content.Server.DeadSpace.Demons.Shadowling;

public sealed class ShadowlingSystem : SharedShadowlingSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float ConeHalfAngle = 60f * MathF.PI / 180f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ShadowlingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            comp.Accumulator += frameTime;
            if (comp.Accumulator < comp.HealingInterval) continue;
            comp.Accumulator -= comp.HealingInterval;

            var currentLight = CalculateTotalLight(uid);
            comp.IsInDarkness = currentLight <= comp.Threshold;
            _movement.RefreshMovementSpeedModifiers(uid);

            if (comp.IsInDarkness)
            {
                _damageable.TryChangeDamage(uid, comp.PassiveHealing, true);
            }
            else
            {
                var damage = new DamageSpecifier();
                var heatUron = 5f + Math.Clamp((currentLight - comp.Threshold) * 10f, 0f, 10f);
                damage.DamageDict.Add("Heat", heatUron);
                _damageable.TryChangeDamage(uid, damage, true);
                _popup.PopupEntity("Свет выжигает вас!", uid, uid, PopupType.LargeCaution);
                _audio.PlayPvs(new SoundCollectionSpecifier("ShadowlingBurnDamage"), uid);
            }
        }
    }

    private float CalculateTotalLight(EntityUid uid)
    {
        float totalLight = 0f;
        var shadowlingPos = _transform.GetWorldPosition(uid);
        var mapPos = _transform.GetMapCoordinates(uid);
        var lights = _lookup.GetEntitiesInRange<PointLightComponent>(mapPos, 15f);
        foreach (var lightUid in lights)
        {
            if (!TryComp<PointLightComponent>(lightUid, out var light) || !light.Enabled) continue;

            var lightPos = _transform.GetWorldPosition(lightUid);
            var distance = (shadowlingPos - lightPos).Length();

            if (distance > light.Radius)
                continue;

            if (light.MaskPath != null)
            {
                var directionToShadowling = shadowlingPos - lightPos;

                if (directionToShadowling.LengthSquared() < 0.01f)
                {
                    totalLight += light.Energy;
                    continue;
                }

                var lightRotation = _transform.GetWorldRotation(lightUid);
                var forward = lightRotation.ToWorldVec();
                var angle = MathF.Acos(Vector2.Dot(forward, directionToShadowling.Normalized()));

                if (angle > ConeHalfAngle)
                    continue;
            }

            if (!_examine.InRangeUnOccluded(uid, lightUid, light.Radius))
                continue;

            totalLight += (1.0f - distance / light.Radius) * light.Energy;
        }
        return totalLight;
    }

    private void OnRefreshSpeed(EntityUid uid, ShadowlingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.IsInDarkness)
            args.ModifySpeed(component.SpeedMultiplier, component.SpeedMultiplier);
    }
}