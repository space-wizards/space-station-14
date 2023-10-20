// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Actions;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkReaper;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.SS220.DarkReaper;

public sealed class DarkReaperSystem : SharedDarkReaperSystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("DarkReaper");

    private const int MaxBooEntities = 30;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void ChangeForm(EntityUid uid, DarkReaperComponent comp, bool isMaterial)
    {
        var isTransitioning = comp.PhysicalForm != isMaterial;
        base.ChangeForm(uid, comp, isMaterial);

        if (isTransitioning && !isMaterial)
        {
            if (comp.ActivePortal != null)
            {
                QueueDel(comp.ActivePortal);
                comp.ActivePortal = null;
            }
        }
    }

    protected override void CreatePortal(EntityUid uid, DarkReaperComponent comp)
    {
        base.CreatePortal(uid, comp);

        // Make lights blink
        BooInRadius(uid, 6);
    }

    protected override void OnAfterConsumed(EntityUid uid, DarkReaperComponent comp, AfterConsumed args)
    {
        base.OnAfterConsumed(uid, comp, args);

        if (!args.Cancelled && args.Target is EntityUid target)
        {
            if (comp.PhysicalForm && target.IsValid() && !EntityManager.IsQueuedForDeletion(target) && _mobState.IsDead(target))
            {
                if (_container.TryGetContainer(uid, DarkReaperComponent.ConsumedContainerId, out var container))
                {
                    container.Insert(target);
                }

                _damageable.TryChangeDamage(uid, comp.HealPerConsume, true, origin: args.Args.User);

                comp.Consumed++;
                UpdateStage(uid, comp);
                UpdateAlert(uid, comp);
                Dirty(uid, comp);
            }
        }
    }

    private void UpdateAlert(EntityUid uid, DarkReaperComponent comp)
    {
        _alerts.ClearAlert(uid, AlertType.DeadscoreStage1);
        _alerts.ClearAlert(uid, AlertType.DeadscoreStage2);

        AlertType alert;
        if (comp.CurrentStage == 1)
            alert = AlertType.DeadscoreStage1;
        else if (comp.CurrentStage == 2)
            alert = AlertType.DeadscoreStage2;
        else
        {
            return;
        }

        if (!comp.ConsumedPerStage.TryGetValue(comp.CurrentStage - 1, out var severity))
            severity = 0;

        severity -= comp.Consumed;

        if (alert == AlertType.DeadscoreStage1 && severity > 3)
        {
            severity = 3; // 3 is a max value our sprite can display at stage 1
            _sawmill.Error("Had to clamp alert severity. It shouldn't happen. Report it to Artur.");
        }
        else if (alert == AlertType.DeadscoreStage2 && severity > 8)
        {
            severity = 8; // 8 is a max value our sprite can display at stage 2
            _sawmill.Error("Had to clamp alert severity. It shouldn't happen. Report it to Artur.");
        }

        if (severity <= 0)
        {
            _alerts.ClearAlert(uid, AlertType.DeadscoreStage1);
            _alerts.ClearAlert(uid, AlertType.DeadscoreStage2);
            return;
        }

        _alerts.ShowAlert(uid, alert, (short) severity);
    }

    protected override void OnCompInit(EntityUid uid, DarkReaperComponent comp, ComponentStartup args)
    {
        base.OnCompInit(uid, comp, args);

        _container.EnsureContainer<Container>(uid, DarkReaperComponent.ConsumedContainerId);

        if (!comp.RoflActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.RoflActionEntity, comp.RoflAction);

        if (!comp.StunActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.StunActionEntity, comp.StunAction);

        if (!comp.ConsumeActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.ConsumeActionEntity, comp.ConsumeAction);

        if (!comp.MaterializeActionEntity.HasValue)
            _actions.AddAction(uid, ref comp.MaterializeActionEntity, comp.MaterializeAction);

        UpdateAlert(uid, comp);
    }

    protected override void OnCompShutdown(EntityUid uid, DarkReaperComponent comp, ComponentShutdown args)
    {
        base.OnCompShutdown(uid, comp, args);

        _actions.RemoveAction(uid, comp.RoflActionEntity);
        _actions.RemoveAction(uid, comp.StunActionEntity);
        _actions.RemoveAction(uid, comp.ConsumeActionEntity);
        _actions.RemoveAction(uid, comp.MaterializeActionEntity);
    }

    protected override void DoStunAbility(EntityUid uid, DarkReaperComponent comp)
    {
        base.DoStunAbility(uid, comp);

        // Destroy lights in radius
        var lightQuery = GetEntityQuery<PoweredLightComponent>();
        var entities = _lookup.GetEntitiesInRange(uid, comp.StunAbilityLightBreakRadius);

        foreach (var entity in entities)
        {
            if (!lightQuery.TryGetComponent(entity, out var lightComp))
                continue;

            _poweredLight.TryDestroyBulb(entity, lightComp);
        }
    }

    private void BooInRadius(EntityUid uid, float radius)
    {
        var entities = _lookup.GetEntitiesInRange(uid, radius);

        var booCounter = 0;
        foreach (var ent in entities)
        {
            var handled = _ghost.DoGhostBooEvent(ent);

            if (handled)
                booCounter++;

            if (booCounter >= MaxBooEntities)
                break;
        }
    }

    protected override void DoRoflAbility(EntityUid uid, DarkReaperComponent comp)
    {
        base.DoRoflAbility(uid, comp);

        // Make lights blink
        BooInRadius(uid, 6);
    }
}
