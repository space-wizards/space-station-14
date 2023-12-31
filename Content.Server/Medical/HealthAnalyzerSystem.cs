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
using System.Linq;

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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<HealthAnalyzerComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<HealthAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
            SubscribeLocalEvent<HealthAnalyzerComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        }

        private void OnAfterInteract(Entity<HealthAnalyzerComponent> entity, ref AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(entity.Owner, user: args.User))
                return;

            _audio.PlayPvs(entity.Comp.ScanningBeginSound, entity);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(entity.Comp.ScanDelay), new HealthAnalyzerDoAfterEvent(), entity.Owner, target: args.Target, used: entity.Owner)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });
        }

        /// <summary>
        /// Turn off when placed into a storage item or moved between slots/hands
        /// </summary>
        private void OnInsertedIntoContainer(EntityUid entity, HealthAnalyzerComponent component, EntGotInsertedIntoContainerMessage args)
        {
            if (component.ScannedEntity.HasValue)
                StopAnalyzingEntity(component.ScannedEntity.Value, (Entity<HealthAnalyzerComponent>)(entity, component));
        }

        /// <summary>
        /// Disable continuous updates once battery is dead
        /// </summary>
        private void OnPowerCellSlotEmpty(Entity<HealthAnalyzerComponent> entity, ref PowerCellSlotEmptyEvent args)
        {
            if (entity.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(entity.Comp.ScannedEntity.Value, entity);
        }

        /// <summary>
        /// Turn off the analyser when dropped
        /// </summary>
        private void OnDropped(EntityUid entity, HealthAnalyzerComponent component, DroppedEvent args)
        {
            if (component.ScannedEntity.HasValue)
                StopAnalyzingEntity(component.ScannedEntity.Value, (Entity<HealthAnalyzerComponent>) (entity, component));
        }

        private void OnDoAfter(Entity<HealthAnalyzerComponent> entity, ref HealthAnalyzerDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(entity.Owner, user: args.User))
                return;

            _audio.PlayPvs(entity.Comp.ScanningEndSound, args.User);

            //If we were already analyzing someone, stop
            if (entity.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(entity.Comp.ScannedEntity.Value, entity);

            OpenUserInterface(args.User, entity.Owner);
            BeginAnalyzingEntity(args.Target.Value, entity);
            args.Handled = true;
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
        private void BeginAnalyzingEntity(EntityUid uid, Entity<HealthAnalyzerComponent> healthAnalyzer)
        {
            var healthBeingAnalyzedComponent = EnsureComp<HealthBeingAnalyzedComponent>(uid);

            healthBeingAnalyzedComponent.ActiveAnalyzers.Add(healthAnalyzer);

            //Link the health analyzer to the scanned entity
            healthAnalyzer.Comp.ScannedEntity = uid;
            _cell.SetPowerCellDrawEnabled(healthAnalyzer.Owner, true);

            UpdateScannedUser(healthAnalyzer.Owner, uid, true);
        }

        /// <summary>
        /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
        /// </summary>
        private void StopAnalyzingEntity(EntityUid uid, Entity<HealthAnalyzerComponent> healthAnalyzer)
        {
            if (!TryComp<HealthBeingAnalyzedComponent>(uid, out var healthBeingAnalyzedComponent))
                return;

            //If there is more than 1 analyzer currently monitoring this entity, just remove from the list
            healthBeingAnalyzedComponent.ActiveAnalyzers.Remove(healthAnalyzer);

            //If we were the last, remove the component
            if (healthBeingAnalyzedComponent.ActiveAnalyzers.Count == 0)
                RemCompDeferred<HealthBeingAnalyzedComponent>(uid);

            //Unlink the analyzer
            healthAnalyzer.Comp.ScannedEntity = null;

            UpdateScannedUser(healthAnalyzer.Owner, uid, false);
            _cell.SetPowerCellDrawEnabled(uid, false);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<HealthBeingAnalyzedComponent>();
            while (query.MoveNext(out var playerUid, out var healthBeingAnalyzedComponent))
            {
                //If the component is somehow orphaned, remove it
                if (healthBeingAnalyzedComponent.ActiveAnalyzers.Count == 0)
                    RemCompDeferred<HealthBeingAnalyzedComponent>(playerUid);

                //Has it been 1 second since the last update?
                var updateTimerElapsed = healthBeingAnalyzedComponent.TimeSinceLastUpdate > 1f;
                if (!updateTimerElapsed)
                {
                    //Increment timer
                    healthBeingAnalyzedComponent.TimeSinceLastUpdate += frameTime;
                    continue;
                }

                foreach (var healthAnalyzer in healthBeingAnalyzedComponent.ActiveAnalyzers)
                {
                    //Remove if this analyzer is being deleted - or is not an analyzer
                    if (LifeStage(healthAnalyzer.Owner) >= EntityLifeStage.Terminating)
                    {
                        StopAnalyzingEntity(playerUid, healthAnalyzer);
                        continue;
                    }

                    //Get distance between health analyzer and the scanned entity
                    var scannedEntityPosition = _transformSystem.GetMapCoordinates(playerUid);
                    var healthAnalyserPosition = _transformSystem.GetMapCoordinates(healthAnalyzer.Owner);
                    var distance = (scannedEntityPosition.Position - healthAnalyserPosition.Position).Length();

                    if (scannedEntityPosition.MapId != healthAnalyserPosition.MapId || distance > healthAnalyzer.Comp.MaxScanRange)
                    {
                        //Range too far, disable updates
                        StopAnalyzingEntity(playerUid, healthAnalyzer);
                        continue;
                    }

                    UpdateScannedUser(healthAnalyzer, playerUid, true);
                }

                //Reset timer
                healthBeingAnalyzedComponent.TimeSinceLastUpdate = 0;
            }
        }

        public void UpdateScannedUser(EntityUid healthAnalyzerUid, EntityUid? target, bool scanMode)
        {
            if (target == null || !_uiSystem.TryGetUi(healthAnalyzerUid, HealthAnalyzerUiKey.Key, out var ui))
                return;

            if (!HasComp<DamageableComponent>(target))
                return;

            float bodyTemperature;
            if (TryComp<TemperatureComponent>(target, out var temp))
                bodyTemperature = temp.CurrentTemperature;
            else
                bodyTemperature = float.NaN;

            float bloodAmount;
            if (TryComp<BloodstreamComponent>(target, out var bloodstream) &&
                _solutionContainerSystem.ResolveSolution(target.Value, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
                bloodAmount = bloodSolution.FillFraction;
            else
                bloodAmount = float.NaN;

            _uiSystem.SendUiMessage(ui, new HealthAnalyzerScannedUserMessage(
                GetNetEntity(target),
                bodyTemperature,
                bloodAmount,
                scanMode
            ));
        }
    }
}
