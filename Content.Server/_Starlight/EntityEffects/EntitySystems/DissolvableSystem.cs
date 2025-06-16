using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Starlight.EntityEffects.Components;
using Content.Shared.Starlight.EntityEffects.EntitySystems;
using Content.Shared.Starlight.EntityEffects;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Temperature;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Starlight.EntityEffects.EntitySystems;

public sealed class DissolvableSystem : SharedDissolvableSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly TemperatureSystem _temperatureSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private readonly Dictionary<Entity<DissolvableComponent>, float> _dissolveEvets = new();

    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(AtmosphereSystem));
        
        SubscribeLocalEvent<ThermiteComponent, InteractUsingEvent>(OnInteractUsing);
        
        SubscribeLocalEvent<DissolvableComponent, DestructionEventArgs>(OnDestruction);
    }
    
    private void OnInteractUsing(EntityUid uid, ThermiteComponent thermite, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(args.Used, isHotEvent);

        if (!isHotEvent.IsHot && thermite.requiredTag == null || !_tagSystem.HasTag(args.Used, thermite.requiredTag!))
            return;
        
        foreach (var dissolvable in _lookupSystem.GetEntitiesInRange<DissolvableComponent>(_transform.GetMapCoordinates(uid), 1f))
        {
            if (dissolvable.Comp.DissolveStacks > 0 && !dissolvable.Comp.OnDissolve)
                Dissolve(dissolvable.Owner, args.Used, dissolvable.Comp, args.User);
        }
        
        foreach (var thermiteEnt in _lookupSystem.GetEntitiesInRange<ThermiteComponent>(_transform.GetMapCoordinates(uid), 1f))
            EntityManager.QueueDeleteEntity(thermiteEnt.Owner);
        
        args.Handled = true;
    }
    
    private void OnDestruction(EntityUid uid, DissolvableComponent dissolvable, DestructionEventArgs args)
    {
        Extinguish(uid, dissolvable);
    }

    public void Dissolve(EntityUid uid, EntityUid dissolveSource, DissolvableComponent? dissolvable = null, EntityUid? dissolveSourceUser = null)
    {
        if (!Resolve(uid, ref dissolvable))
            return;

        if (dissolvable.AlwaysCombustible)
            dissolvable.DissolveStacks = Math.Max(dissolvable.DissolveStacksOnIgnite, dissolvable.DissolveStacks);

        if (dissolvable.DissolveStacks > 0 && !dissolvable.OnDissolve)
        {
            if (dissolveSourceUser != null)
                _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on dissolve by {ToPrettyString(dissolveSourceUser.Value):actor} with {ToPrettyString(dissolveSource):tool}");
            else
                _adminLogger.Add(LogType.Flammable, $"{ToPrettyString(uid):target} set on dissolve by {ToPrettyString(dissolveSource):actor}");
            dissolvable.OnDissolve = true;
            
            dissolvable.Effect = Spawn("ThermiteFire", _transform.GetMapCoordinates(uid));

            var extinguished = new IgnitedEvent();
            RaiseLocalEvent(uid, ref extinguished);
        }

        UpdateAppearance(uid, dissolvable);
    }

    public void Resist(EntityUid uid, DissolvableComponent? dissolvable = null)
    {
        if (!Resolve(uid, ref dissolvable))
            return;

        if (!dissolvable.OnDissolve || !_actionBlockerSystem.CanInteract(uid, null) || dissolvable.Resisting)
            return;

        dissolvable.Resisting = true;

        _popup.PopupEntity(Loc.GetString("dissolvable-component-resist-message"), uid, uid);
        _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(2f), true);

        dissolvable.ResistingStartedOn = _timing.CurTime;

    }

    public override void Update(float frameTime)
    {
        foreach (var (dissolvable, deltaTemp) in _dissolveEvets)
        {
            // 100 -> 1, 200 -> 2, 400 -> 3...
            var dissolveStackMod = Math.Max(MathF.Log2(deltaTemp / 100) + 1, 0);
            var dissolveStackDelta = dissolveStackMod - dissolvable.Comp.DissolveStacks;
            var dissolveableEntity = dissolvable.Owner;
            if (dissolveStackDelta > 0)
                AdjustDissolveStacks(dissolveableEntity, dissolveStackDelta, dissolvable);
            Dissolve(dissolveableEntity, dissolveableEntity, dissolvable);
        }
        _dissolveEvets.Clear();
        
        var toProcess = new List<EntityUid>();

        var query = EntityQueryEnumerator<DissolvableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dissolvable, out _))
        {
            toProcess.Add(uid);
        }

        foreach (var uid in toProcess)
        {
            if (!TryComp(uid, out DissolvableComponent? dissolvable))
                continue;

            if (_timing.CurTime < dissolvable.LastTimeUpdated + dissolvable.UpdateDelay)
                continue;

            dissolvable.LastTimeUpdated = _timing.CurTime;

            if (dissolvable.Resisting && dissolvable.ResistingStartedOn != null && dissolvable.ResistingStartedOn + dissolvable.ResistingTime <= _timing.CurTime)
            {
                dissolvable.Resisting = false;
                dissolvable.DissolveStacks -= 1f;
                UpdateAppearance(uid, dissolvable);
            }

            if (dissolvable.DissolveStacks < 0)
                dissolvable.DissolveStacks = MathF.Min(0, dissolvable.DissolveStacks + 1);

            if (dissolvable.DissolveStacks > 0)
            {
                if (TryComp(uid, out TemperatureComponent? temp))
                    _temperatureSystem.ChangeHeat(uid, 12500 * dissolvable.DissolveStacks, false, temp);

                _damageableSystem.TryChangeDamage(uid, dissolvable.Damage * dissolvable.DissolveStacks, interruptsDoAfters: false);

                AdjustDissolveStacks(uid, dissolvable.DissolveStacksFade * (dissolvable.Resisting ? 10f : 1f), dissolvable, dissolvable.OnDissolve);
            }
            else
            {
                Extinguish(uid, dissolvable);
            }
        }
    }
}