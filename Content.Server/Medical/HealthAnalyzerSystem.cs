using Content.Server.Body.Components;
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

        private void OnAfterInteract(EntityUid uid, HealthAnalyzerComponent healthAnalyzer, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(uid, user: args.User))
                return;

            _audio.PlayPvs(healthAnalyzer.ScanningBeginSound, uid);

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, healthAnalyzer.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
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

        private void OnDoAfter(EntityUid uid, HealthAnalyzerComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.HasDrawCharge(uid, user: args.User))
                return;

            _audio.PlayPvs(component.ScanningEndSound, args.Args.User);

            //If we were already analyzing someone, stop
            if (component.ScannedEntity.HasValue)
                StopAnalyzingEntity(component.ScannedEntity.Value, uid, component);

            BeginAnalyzingEntity(args.Args.Target.Value, uid, component);

            _cell.SetPowerCellDrawEnabled(uid, true);
            OpenUserInterface(args.Args.User, uid);
            UpdateScannedUser(uid, args.Args.Target.Value);
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
        private void BeginAnalyzingEntity(EntityUid uid, EntityUid healthAnalyzerUid, HealthAnalyzerComponent? component = null, HealthBeingAnalyzedComponent? healthBeingAnalyzedComponent = null)
        {
            if (!Resolve(healthAnalyzerUid, ref component))
                return;

            if (!Resolve(uid, ref healthBeingAnalyzedComponent, false))
            {
                //No other active analyzers, create the component and attach to the scanned entity
                healthBeingAnalyzedComponent = new HealthBeingAnalyzedComponent();
                AddComp(uid, healthBeingAnalyzedComponent);
            }

            if (!healthBeingAnalyzedComponent.ActiveAnalyzers.Contains(healthAnalyzerUid))
                healthBeingAnalyzedComponent.ActiveAnalyzers.Add(healthAnalyzerUid);

            component.ScannedEntity = uid;
        }

        /// <summary>
        /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
        /// </summary>
        private void StopAnalyzingEntity(EntityUid uid, EntityUid healthAnalyzerUid, HealthAnalyzerComponent? component = null, HealthBeingAnalyzedComponent? healthBeingAnalyzedComponent = null)
        {
            if (!Resolve(healthAnalyzerUid, ref component))
                return;

            if (!Resolve(uid, ref healthBeingAnalyzedComponent, false))
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

            component.ScannedEntity = null;
        }

        private void OnDamageChanged(EntityUid damagedEntityUid, HealthBeingAnalyzedComponent component, DamageChangedEvent args)
        {
            foreach (var healthAnalyzerUid in component.ActiveAnalyzers)
            {
                if (!TryComp<HealthAnalyzerComponent>(healthAnalyzerUid, out var healthAnalyzerComp))
                    continue;

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

            TryComp<TemperatureComponent>(target, out var temp);
            TryComp<BloodstreamComponent>(target, out var bloodstream);

            _uiSystem.SendUiMessage(ui, new HealthAnalyzerScannedUserMessage(GetNetEntity(target), temp != null ? temp.CurrentTemperature : float.NaN,
                bloodstream != null ? bloodstream.BloodSolution.FillFraction : float.NaN));
        }
    }
}
