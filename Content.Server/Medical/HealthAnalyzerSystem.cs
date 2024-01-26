using Content.Server.Body.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.PowerCell;
using Content.Server.Temperature.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.PowerCell;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Medical
{
    public sealed class HealthAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly PowerCellSystem _cell = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActiveHealthMonitoredComponent, ComponentStartup>(OnActiveHealthMonitoredStartup);

            SubscribeLocalEvent<HealthAnalyzerComponent, ComponentShutdown>(OnHealthAnalyserShutdown);
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<HealthAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
            SubscribeLocalEvent<HealthAnalyzerComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
            SubscribeLocalEvent<HealthAnalyzerComponent, DroppedEvent>(OnDropped);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveHealthMonitoredComponent, TransformComponent>();
            while (query.MoveNext(out var entityUid, out var activeHealthComponent, out var entityTransform))
            {
                //If the component is somehow orphaned, remove it
                if (activeHealthComponent.ActiveAnalyzers.Count == 0)
                    RemCompDeferred<ActiveHealthMonitoredComponent>(entityUid);

                //Update rate limited to 1 second
                if (activeHealthComponent.NextUpdate > _timing.CurTime)
                    continue;

                activeHealthComponent.NextUpdate = _timing.CurTime + activeHealthComponent.UpdateInterval;

                //Send updates to each health analyzer every cref
                foreach (var healthAnalyzer in activeHealthComponent.ActiveAnalyzers)
                {
                    //Ensure only health analyzers
                    if (!TryComp<HealthAnalyzerComponent>(healthAnalyzer, out var healthAnalyzerComponent))
                    {
                        StopAnalyzingEntity(entityUid, healthAnalyzer, activeHealthComponent);
                        continue;
                    }

                    //Get distance between health analyzer and the scanned entity
                    var healthAnalyserPosition = Transform(healthAnalyzer).Coordinates;

                    if (!healthAnalyserPosition.InRange(EntityManager, _transformSystem, entityTransform.Coordinates, healthAnalyzerComponent.MaxScanRange))
                    {
                        //Range too far, disable updates
                        StopAnalyzingEntity(entityUid, healthAnalyzer, activeHealthComponent, healthAnalyzerComponent);
                        continue;
                    }

                    UpdateScannedUser(healthAnalyzer, entityUid, true);
                }
            }
        }

        /// <summary>
        /// Sets the next update time
        /// </summary>
        private void OnActiveHealthMonitoredStartup(EntityUid uid, ActiveHealthMonitoredComponent component, ComponentStartup args)
        {
            component.NextUpdate = _timing.CurTime + component.UpdateInterval;
        }

        /// <summary>
        /// Stop analyzing when the health analyzer is removed
        /// </summary>
        private void OnHealthAnalyserShutdown(Entity<HealthAnalyzerComponent> healthAnalyzer, ref ComponentShutdown args)
        {
            if (healthAnalyzer.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(healthAnalyzer.Comp.ScannedEntity.Value, healthAnalyzer);
        }

        /// <summary>
        /// Trigger the doafter for scanning
        /// </summary>
        private void OnAfterInteract(Entity<HealthAnalyzerComponent> healthAnalyzer, ref AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(healthAnalyzer, user: args.User))
                return;

            _audio.PlayPvs(healthAnalyzer.Comp.ScanningBeginSound, healthAnalyzer);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, healthAnalyzer.Comp.ScanDelay, new HealthAnalyzerDoAfterEvent(), healthAnalyzer, target: args.Target, used: healthAnalyzer)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }

        private void OnDoAfter(Entity<HealthAnalyzerComponent> healthAnalyzer, ref HealthAnalyzerDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(healthAnalyzer, user: args.User))
                return;

            _audio.PlayPvs(healthAnalyzer.Comp.ScanningEndSound, healthAnalyzer);

            //If we were already analyzing someone, stop
            if (healthAnalyzer.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(healthAnalyzer.Comp.ScannedEntity.Value, healthAnalyzer, healthAnalyzerComponent: healthAnalyzer.Comp);

            OpenUserInterface(args.User, healthAnalyzer);
            BeginAnalyzingEntity(args.Target.Value, healthAnalyzer);
            args.Handled = true;
        }

        /// <summary>
        /// Turn off when placed into a storage item or moved between slots/hands
        /// </summary>
        private void OnInsertedIntoContainer(Entity<HealthAnalyzerComponent> healthAnalyzer, ref EntGotInsertedIntoContainerMessage args)
        {
            if (healthAnalyzer.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(healthAnalyzer.Comp.ScannedEntity.Value, healthAnalyzer);
        }

        /// <summary>
        /// Disable continuous updates once battery is dead
        /// </summary>
        private void OnPowerCellSlotEmpty(Entity<HealthAnalyzerComponent> healthAnalyzer, ref PowerCellSlotEmptyEvent args)
        {
            if (healthAnalyzer.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(healthAnalyzer.Comp.ScannedEntity.Value, healthAnalyzer);
        }

        /// <summary>
        /// Turn off the analyser when dropped
        /// </summary>
        private void OnDropped(Entity<HealthAnalyzerComponent> healthAnalyzer, ref DroppedEvent args)
        {
            if (healthAnalyzer.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(healthAnalyzer.Comp.ScannedEntity.Value, healthAnalyzer);
        }

        private void OpenUserInterface(EntityUid user, EntityUid analyzer)
        {
            if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.TryGetUi(analyzer, HealthAnalyzerUiKey.Key, out var ui))
                return;

            _uiSystem.OpenUi(ui, actor.PlayerSession);
        }

        /// <summary>
        /// Mark the entity as having its health analyzed, and link the analyzer to it
        /// </summary>
        /// <param name="target">The entity to start analyzing</param>
        /// <param name="healthAnalyzer">The health analyzer that should receive the updates</param>
        private void BeginAnalyzingEntity(EntityUid target, Entity<HealthAnalyzerComponent> healthAnalyzer)
        {
            var healthBeingAnalyzedComponent = EnsureComp<ActiveHealthMonitoredComponent>(target);

            healthBeingAnalyzedComponent.ActiveAnalyzers.Add(healthAnalyzer);

            //Link the health analyzer to the scanned entity
            healthAnalyzer.Comp.ScannedEntity = target;

            _cell.SetPowerCellDrawEnabled(healthAnalyzer.Owner, true);

            UpdateScannedUser(healthAnalyzer.Owner, target, true);
        }

        /// <summary>
        /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
        /// </summary>
        /// <param name="target">The entity to analyzing</param>
        /// <param name="healthAnalyzer">The health analyzer that was receiving the updates</param>
        /// <param name="healthAnalyzerComponent">Optional health analyzer component</param>
        private void StopAnalyzingEntity(EntityUid target, EntityUid healthAnalyzer, ActiveHealthMonitoredComponent? healthMonitoredComponent = null, HealthAnalyzerComponent? healthAnalyzerComponent = null)
        {
            if (!Resolve(target, ref healthMonitoredComponent, false))
                return;

            //Remove analyzer from the list
            healthMonitoredComponent.ActiveAnalyzers.Remove(healthAnalyzer);

            //If we were the last, remove the component
            if (healthMonitoredComponent.ActiveAnalyzers.Count == 0)
                RemCompDeferred<ActiveHealthMonitoredComponent>(target);

            //If somehow healthAnalyzer is not a health analyzer, just skip the rest
            if (!Resolve(healthAnalyzer, ref healthAnalyzerComponent))
                return;

            //Unlink the analyzer
            healthAnalyzerComponent.ScannedEntity = null;

            UpdateScannedUser(healthAnalyzer, target, false);
            _cell.SetPowerCellDrawEnabled(target, false);
        }

        /// <summary>
        /// Send an update for the target to the healthAnalyzer
        /// </summary>
        /// <param name="healthAnalyzer"></param>
        /// <param name="target"></param>
        /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
        public void UpdateScannedUser(EntityUid healthAnalyzer, EntityUid target, bool scanMode)
        {
            if (!_uiSystem.TryGetUi(healthAnalyzer, HealthAnalyzerUiKey.Key, out var ui))
                return;

            if (!HasComp<DamageableComponent>(target))
                return;

            var bodyTemperature = float.NaN;

            if (TryComp<TemperatureComponent>(target, out var temp))
                bodyTemperature = temp.CurrentTemperature;

            var bloodAmount = float.NaN;

            if (TryComp<BloodstreamComponent>(target, out var bloodstream) &&
                bloodstream != null &&
                _solutionContainerSystem.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                bloodAmount = bloodSolution.FillFraction;

            _uiSystem.SendUiMessage(ui, new HealthAnalyzerScannedUserMessage(
                GetNetEntity(target),
                bodyTemperature,
                bloodAmount,
                scanMode
            ));
        }
    }
}
