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

            SubscribeLocalEvent<HealthBeingAnalyzedComponent, DamageChangedEvent>(OnDamageChanged);
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
        private void OnInsertedIntoContainer(EntityUid uid, HealthAnalyzerComponent component, EntGotInsertedIntoContainerMessage args)
        {
            if (component.ScannedEntity.HasValue)
            {
                StopAnalyzingEntity(component.ScannedEntity.Value, uid, component);
                _cell.SetPowerCellDrawEnabled(uid, false);
            }
        }

        /// <summary>
        /// Disable continuous updates once battery is dead
        /// </summary>
        private void OnPowerCellSlotEmpty(EntityUid uid, HealthAnalyzerComponent component, ref PowerCellSlotEmptyEvent args)
        {
            if (component.ScannedEntity.HasValue)
            {
                StopAnalyzingEntity(component.ScannedEntity.Value, uid, component);
            }
        }

        /// <summary>
        /// Turn off the analyser when dropped
        /// </summary>
        private void OnDropped(EntityUid uid, HealthAnalyzerComponent component, DroppedEvent args)
        {
            if (component.ScannedEntity.HasValue)
            {
                StopAnalyzingEntity(component.ScannedEntity.Value, uid, component);
                _cell.SetPowerCellDrawEnabled(uid, false);
            }
        }

        private void OnDoAfter(Entity<HealthAnalyzerComponent> entity, ref HealthAnalyzerDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(entity.Owner, user: args.User))
                return;

            _audio.PlayPvs(entity.Comp.ScanningEndSound, args.User);

            //If we were already analyzing someone, stop
            if (entity.Comp.ScannedEntity.HasValue)
                StopAnalyzingEntity(entity.Comp.ScannedEntity.Value, entity.Owner, entity.Comp);

            BeginAnalyzingEntity(args.Args.Target.Value, entity.Owner, entity.Comp);

            _cell.SetPowerCellDrawEnabled(uid, true);
            OpenUserInterface(args.Args.User, uid);
            UpdateScannedUser(entity.User, args.Target.Value);
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
        private void BeginAnalyzingEntity(EntityUid uid, EntityUid healthAnalyzerUid, HealthAnalyzerComponent healthAnalyzerComponent)
        {
            var healthBeingAnalyzedComponent = EnsureComp<HealthBeingAnalyzedComponent>(uid);

            if (!healthBeingAnalyzedComponent.ActiveAnalyzers.Contains(healthAnalyzerUid))
                healthBeingAnalyzedComponent.ActiveAnalyzers.Add(healthAnalyzerUid);

            //Link the health analyzer to the scanned entity
            healthAnalyzerComponent.ScannedEntity = uid;
        }

        /// <summary>
        /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
        /// </summary>
        private void StopAnalyzingEntity(EntityUid uid, EntityUid healthAnalyzerUid, HealthAnalyzerComponent? healthAnalyzerComponent = null)
        {
            if (!TryComp<HealthBeingAnalyzedComponent>(uid, out var healthBeingAnalyzedComponent))
                return;

            //If there is more than 1 analyzer currently monitoring this entity, just remove from the list
            if (healthBeingAnalyzedComponent.ActiveAnalyzers.Count > 1)
            {
                healthBeingAnalyzedComponent.ActiveAnalyzers.Remove(healthAnalyzerUid);
            }
            else
            {
                //If we are the last, remove the component
                RemComp<HealthBeingAnalyzedComponent>(uid);
            }


            //Unlink the analyzer if it still exists
            if (Resolve<HealthAnalyzerComponent>(healthAnalyzerUid, ref healthAnalyzerComponent, false))
                healthAnalyzerComponent.ScannedEntity = null;
        }

        private void OnDamageChanged(EntityUid damagedEntityUid, HealthBeingAnalyzedComponent component, DamageChangedEvent args)
        {

            //On the off chance that somehow this component is left behing with no entries - remove it
            //Dont want this eating cpu cycles for no reason
            if (component.ActiveAnalyzers.Count == 0)
            {
                RemComp<HealthBeingAnalyzedComponent>(damagedEntityUid);
                return;
            }

            foreach (var healthAnalyzerUid in component.ActiveAnalyzers)
            {
                //Strangeness catchall
                //Should catch admin deleting the health analyzer, or something thats not an analyser ending up in the list
                if (LifeStage(healthAnalyzerUid) >= EntityLifeStage.Terminating || !TryComp<HealthAnalyzerComponent>(healthAnalyzerUid, out var healthAnalyzerComp))
                {
                    StopAnalyzingEntity(damagedEntityUid, healthAnalyzerUid);
                    continue;
                }

                //Get distance between health analyzer and the scanned entity
                var scannedEntityPosition = _transformSystem.GetMapCoordinates(damagedEntityUid);
                var healthAnalyserPosition = _transformSystem.GetMapCoordinates(healthAnalyzerUid);
                var distance = (scannedEntityPosition.Position - healthAnalyserPosition.Position).Length();

                if (scannedEntityPosition.MapId != healthAnalyserPosition.MapId ||
                    distance > healthAnalyzerComp.MaxScanRange)
                {
                    //Range too far, disable updates
                    StopAnalyzingEntity(damagedEntityUid, healthAnalyzerUid, healthAnalyzerComp);
                    _cell.SetPowerCellDrawEnabled(healthAnalyzerUid, false);
                }
                else
                {
                    UpdateScannedUser(healthAnalyzerUid, damagedEntityUid);
                }
            }
        }

        public void UpdateScannedUser(EntityUid healthAnalyzerUid, EntityUid? target)
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

            OpenUserInterface(user, uid);

            _uiSystem.SendUiMessage(ui, new HealthAnalyzerScannedUserMessage(
                GetNetEntity(target),
                bodyTemperature,
                bloodAmount
            ));
        }
    }
}
