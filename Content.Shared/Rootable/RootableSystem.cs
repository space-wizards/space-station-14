using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cloning.Events;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Gravity;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Slippery;
using Content.Shared.Toggleable;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Rootable;

/// <summary>
/// Adds an action to toggle rooting to the ground, primarily for the Diona species.
/// Being rooted prevents weighlessness and slipping, but causes any floor contents to transfer its reagents to the bloodstream.
/// </summary>
public sealed class RootableSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _logger = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _blood = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private EntityQuery<PuddleComponent> _puddleQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _puddleQuery = GetEntityQuery<PuddleComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<RootableComponent, MapInitEvent>(OnRootableMapInit);
        SubscribeLocalEvent<RootableComponent, ComponentShutdown>(OnRootableShutdown);
        SubscribeLocalEvent<RootableComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<RootableComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<RootableComponent, ToggleActionEvent>(OnRootableToggle);
        SubscribeLocalEvent<RootableComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RootableComponent, IsWeightlessEvent>(OnIsWeightless);
        SubscribeLocalEvent<RootableComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<RootableComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<RootableComponent, CloningEvent>(OnCloning);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RootableComponent, BloodstreamComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var rooted, out var bloodstream))
        {
            if (!rooted.Rooted || rooted.PuddleEntity == null || curTime < rooted.NextUpdate || !_puddleQuery.TryComp(rooted.PuddleEntity, out var puddleComp))
                continue;

            rooted.NextUpdate += rooted.TransferFrequency;
            Dirty(uid, rooted);
            PuddleReact((uid, rooted, bloodstream), (rooted.PuddleEntity.Value, puddleComp!));
        }
    }

    /// <summary>
    /// Determines if the puddle is set up properly and if so, moves on to reacting.
    /// </summary>
    private void PuddleReact(Entity<RootableComponent, BloodstreamComponent> ent, Entity<PuddleComponent> puddleEntity)
    {
        if (!_solutionContainer.ResolveSolution(puddleEntity.Owner, puddleEntity.Comp.SolutionName, ref puddleEntity.Comp.Solution, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        ReactWithEntity(ent, puddleEntity, solution);
    }

    /// <summary>
    /// Attempt to transfer an amount of the solution to the ent's bloodstream.
    /// </summary>
    private void ReactWithEntity(Entity<RootableComponent, BloodstreamComponent> ent, Entity<PuddleComponent> puddleEntity, Solution solution)
    {
        if (!_solutionContainer.ResolveSolution(ent.Owner, ent.Comp2.ChemicalSolutionName, ref ent.Comp2.ChemicalSolution, out var chemSolution) || chemSolution.AvailableVolume <= 0)
            return;

        var availableTransfer = FixedPoint2.Min(solution.Volume, ent.Comp1.TransferRate);
        var transferAmount = FixedPoint2.Min(availableTransfer, chemSolution.AvailableVolume);
        var transferSolution = _solutionContainer.SplitSolution(puddleEntity.Comp.Solution!.Value, transferAmount);

        _reactive.DoEntityReaction(ent, transferSolution, ReactionMethod.Ingestion);

        // Log solution addition by puddle.
        if (_blood.TryAddToChemicals((ent, ent.Comp2), transferSolution))
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(ent):target} absorbed puddle {SharedSolutionContainerSystem.ToPrettyString(transferSolution)}");
    }

    private void OnCloning(Entity<RootableComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        var cloneComp = EnsureComp<RootableComponent>(args.CloneUid);
        cloneComp.TransferRate = ent.Comp.TransferRate;
        cloneComp.TransferFrequency = ent.Comp.TransferFrequency;
        cloneComp.SpeedModifier = ent.Comp.SpeedModifier;
        cloneComp.RootSound = ent.Comp.RootSound;
        Dirty(args.CloneUid, cloneComp);
    }

    private void OnRootableMapInit(Entity<RootableComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent, out ActionsComponent? comp))
            return;

        ent.Comp.NextUpdate = _timing.CurTime;
        Dirty(ent);
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, component: comp);
    }

    private void OnRootableShutdown(Entity<RootableComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp(ent, out ActionsComponent? comp))
            return;

        var actions = new Entity<ActionsComponent?>(ent, comp);
        _actions.RemoveAction(actions, ent.Comp.ActionEntity);
        _alerts.ClearAlert(ent.Owner, ent.Comp.RootedAlert);
    }

    private void OnRootableToggle(Entity<RootableComponent> ent, ref ToggleActionEvent args)
    {
        args.Handled = TryToggleRooting((ent, ent));
    }

    private void OnMobStateChanged(Entity<RootableComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Rooted)
            TryToggleRooting((ent, ent));
    }

    public bool TryToggleRooting(Entity<RootableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        ent.Comp.Rooted = !ent.Comp.Rooted;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _gravity.RefreshWeightless(ent.Owner);

        if (ent.Comp.Rooted)
        {
            _alerts.ShowAlert(ent.Owner, ent.Comp.RootedAlert);
            var curTime = _timing.CurTime;
            if (curTime > ent.Comp.NextUpdate)
                ent.Comp.NextUpdate = curTime;
        }
        else
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.RootedAlert);
        }

        _audio.PlayPredicted(ent.Comp.RootSound, ent.Owner.ToCoordinates(), ent);
        Dirty(ent);

        return true;
    }

    private void OnIsWeightless(Entity<RootableComponent> ent, ref IsWeightlessEvent args)
    {
        if (args.Handled || !ent.Comp.Rooted)
            return;

        // Do not cancel weightlessness if the person is in off-grid.
        if (!_gravity.EntityOnGravitySupportingGridOrMap(ent.Owner))
            return;

        args.IsWeightless = false;
        args.Handled = true;
    }

    private void OnSlipAttempt(Entity<RootableComponent> ent, ref SlipAttemptEvent args)
    {
        if (!ent.Comp.Rooted)
            return;

        if (args.SlipCausingEntity != null && HasComp<DamageOnTriggerComponent>(args.SlipCausingEntity))
            return;

        args.NoSlip = true;
    }

    private void OnStartCollide(Entity<RootableComponent> ent, ref StartCollideEvent args)
    {
        if (!_puddleQuery.HasComp(args.OtherEntity))
            return;

        ent.Comp.PuddleEntity = args.OtherEntity;

        if (ent.Comp.NextUpdate < _timing.CurTime) // To prevent constantly moving to new puddles resetting the timer.
            ent.Comp.NextUpdate = _timing.CurTime;

        Dirty(ent);
    }

    private void OnEndCollide(Entity<RootableComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.PuddleEntity != args.OtherEntity)
            return;

        var exists = Exists(args.OtherEntity);

        if (!_physicsQuery.TryComp(ent, out var body))
            return;

        foreach (var entContact in _physics.GetContactingEntities(ent, body))
        {
            if (exists && entContact == args.OtherEntity)
                continue;

            if (!_puddleQuery.HasComponent(entContact))
                continue;

            ent.Comp.PuddleEntity = ent;
            return; // New puddle found, no need to continue.
        }

        ent.Comp.PuddleEntity = null;
        Dirty(ent);
    }

    private void OnRefreshMovementSpeed(Entity<RootableComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Rooted)
            args.ModifySpeed(ent.Comp.SpeedModifier);
    }
}
