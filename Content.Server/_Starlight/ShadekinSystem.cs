using Content.Shared.Humanoid;
using Content.Shared.Alert;
using Content.Shared.Actions;
using System.Linq;
using Microsoft.CodeAnalysis;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Content.Shared.Examine;
using Robust.Server.Containers;
using Content.Shared._Starlight;
using Content.Server._Starlight;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Damage;
using Content.Server.Chat.Managers;
using Robust.Shared.Player;
using Content.Shared.Chat;


namespace Content.Server._Floof.Shadekin;

public sealed class ShadekinSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    private sealed class LightCone
    {
        public float Direction { get; set; }
        public float InnerWidth { get; set; }
        public float OuterWidth { get; set; }
    }
    private readonly Dictionary<string, List<LightCone>> lightMasks = new()
    {
        ["/Textures/Effects/LightMasks/cone.png"] = new List<LightCone>
    {
        new LightCone { Direction = 0, InnerWidth = 30, OuterWidth = 60 }
    },
        ["/Textures/Effects/LightMasks/double_cone.png"] = new List<LightCone>
    {
        new LightCone { Direction = 0, InnerWidth = 30, OuterWidth = 60 },
        new LightCone { Direction = 180, InnerWidth = 30, OuterWidth = 60 }
    }
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadekinComponent, ComponentStartup>(OnInit);
        SubscribeLocalEvent<ShadekinComponent, EyeColorInitEvent>(OnEyeColorChange);
        SubscribeLocalEvent<ShadekinComponent, ShadekinAlertEvent>(OnShadekinAlert);
        SubscribeLocalEvent<ShadekinComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnInit(EntityUid uid, ShadekinComponent component, ComponentStartup args)
    {
        UpdateAlert(uid, component);
    }

    private void OnEyeColorChange(EntityUid uid, ShadekinComponent component, EyeColorInitEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        humanoid.EyeColor = Color.Black;
        humanoid.EyeGlowing = true;
        Dirty(uid, humanoid);
    }

    public void UpdateAlert(EntityUid uid, ShadekinComponent component)
    {
        _alerts.ShowAlert(uid, component.ShadekinAlert, (short) component.LightExposure);
    }

    private void OnShadekinAlert(Entity<ShadekinComponent> ent, ref ShadekinAlertEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<ActorComponent>(ent.Owner, out var actor))
        {
            var msg = Loc.GetString("shadekin-alert-" + ent.Comp.LightExposure);
            _chatManager.ChatMessageToOne(ChatChannel.Notifications, msg, msg, EntityUid.Invalid, false, actor.PlayerSession.Channel);
        }        
        args.Handled = true;
    }

    private Angle GetAngle(EntityUid lightUid, SharedPointLightComponent lightComp, EntityUid targetUid)
    {
        var (lightPos, lightRot) = _transform.GetWorldPositionRotation(lightUid);
        lightPos += lightRot.RotateVec(lightComp.Offset);

        var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetUid);

        var mapDiff = targetPos - lightPos;

        var oppositeMapDiff = (-lightRot).RotateVec(mapDiff);
        var angle = oppositeMapDiff.ToWorldAngle();

        if (angle == double.NaN && _transform.ContainsEntity(targetUid, lightUid) || _transform.ContainsEntity(lightUid, targetUid))
        {
            angle = 0f;
        }

        return angle;
    }

    /// <summary>
    /// Return an illumination float value with is how many "energy" of light is hitting our ent.
    /// WARNING: This function might be expensive, Avoid calling it too much and CACHE THE RESULT!
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public float GetLightExposure(EntityUid uid)
    {
        var illumination = 0f;

        var lightQuery = _lookup.GetEntitiesInRange(uid, 20)
                .Where(x => HasComp<PointLightComponent>(x));

        foreach (var light in lightQuery)
        {
            if (_container.IsEntityInContainer(light))
                continue;

            if (!TryComp<PointLightComponent>(light, out var pointLight))
                continue;

            if (HasComp<DarkLightComponent>(light))
                continue;

            if (!pointLight.Enabled
                || pointLight.Radius < 1
                || pointLight.Energy <= 0)
                continue;

            var (lightPos, lightRot) = _transform.GetWorldPositionRotation(light);
            lightPos += lightRot.RotateVec(pointLight.Offset);

            if (!_examine.InRangeUnOccluded(light, uid, pointLight.Radius, null))
                continue;

            Transform(uid).Coordinates.TryDistance(EntityManager, Transform(light).Coordinates, out var dist);

            var denom = dist / pointLight.Radius;
            var attenuation = 1 - (denom * denom);
            var calculatedLight = 0f;

            if (pointLight.MaskPath is not null)
            {
                var angleToTarget = GetAngle(light, pointLight, uid);
                foreach (var cone in lightMasks[pointLight.MaskPath])
                {
                    var coneLight = 0f;
                    var angleAttenuation = (float)Math.Min((float)Math.Max(cone.OuterWidth - angleToTarget, 0f), cone.InnerWidth) / cone.OuterWidth;

                    if (angleToTarget.Degrees - cone.Direction > cone.OuterWidth)
                        continue;
                    else if (angleToTarget.Degrees - cone.Direction > cone.InnerWidth
                        && angleToTarget.Degrees - cone.Direction < cone.OuterWidth)
                        coneLight = pointLight.Energy * attenuation * attenuation * angleAttenuation;
                    else
                        coneLight = pointLight.Energy * attenuation * attenuation;

                    calculatedLight = Math.Max(calculatedLight, coneLight);
                }
            }
            else
                calculatedLight = pointLight.Energy * attenuation * attenuation;

            illumination += calculatedLight; //Math.Max(illumination, calculatedLight);
        }

        return illumination;
    }

    private void SetPassiveBuff(EntityUid uid, float state)
    {
        if (!TryComp<PassiveDamageComponent>(uid, out var passive))
            return;

        if (state >= 2)
        {
            passive.DamageCap = 1;
        }
        else if (state == 1)
        {
            passive.DamageCap = 20;
            passive.AllowedStates.Clear();
            passive.AllowedStates.Add(MobState.Alive);
            passive.Interval = 1f;
        }
        else
        {
            passive.DamageCap = 0;
            passive.AllowedStates.Clear();
            passive.AllowedStates.Add(MobState.Alive);
            passive.AllowedStates.Add(MobState.Critical);
            passive.AllowedStates.Add(MobState.Dead);
            passive.Interval = 0.5f;
        }
    }

    private void ToggleNightVision(EntityUid uid, float state)
    {
        if (state > 0)
            RemComp<NightVisionComponent>(uid);
        else
            EnsureComp<NightVisionComponent>(uid);
    }

    private void ApplyLightDamage(EntityUid uid, float state)
    {
        if (state < 4)
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Heat", 1);
        _damageable.TryChangeDamage(uid, damage, true, false);

    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, ShadekinComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.LightExposure < 3)
            return;

        if (!TryComp<MovementSpeedModifierComponent>(uid, out var movement))
            return;

        var sprintDif = movement.BaseWalkSpeed / movement.BaseSprintSpeed;
        args.ModifySpeed(1f, sprintDif);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadekinComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.Accumulator += frameTime;

            if (_mobState.IsDead(uid))
                continue;

            if (component.Accumulator <= 1)
                continue;

            component.Accumulator = 0;

            var lightExposure = 0f;

            if (!_container.IsEntityInContainer(uid))
                lightExposure = GetLightExposure(uid);

            if (lightExposure >= 15f)
                component.LightExposure = 4;
            else if (lightExposure >= 10f)
                component.LightExposure = 3;
            else if (lightExposure >= 5f)
                component.LightExposure = 2;
            else if (lightExposure >= 0.8f)
                component.LightExposure = 1;
            else
                component.LightExposure = 0;

            SetPassiveBuff(uid, component.LightExposure);
            ToggleNightVision(uid, component.LightExposure);
            ApplyLightDamage(uid, component.LightExposure);
            _speed.RefreshMovementSpeedModifiers(uid);

            UpdateAlert(uid, component);
        }
    }
}