using Content.Server.Medical.Components;
using Content.Shared.Body.Components;
// DS14-start
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DeadSpace.Cloning;
using Content.Shared.EntityConditions.Conditions;
using Content.Shared.EntityEffects.Effects.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.Paper;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.DeadSpace.Medical;
using System.Text;
using Content.Shared.Damage.Prototypes;
// DS14-end
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Server.Body.Systems;

namespace Content.Server.Medical;

public sealed class HealthAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

    // DS14-start
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    // DS14-end

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HealthAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<HealthAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<HealthAnalyzerComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerPrintUiMessage>(OnPrint); // DS14
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<HealthAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            //Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not { } patient)
                continue;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            //Get distance between health analyzer and the scanned entity
            //null is infinite range
            var patientCoordinates = Transform(patient).Coordinates;
            if (component.MaxScanRange != null && !_transformSystem.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange.Value))
            {
                //Range too far, disable updates until they are back in range
                PauseAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.IsAnalyzerActive = true;
            UpdateScannedUser(uid, patient, true);
        }
    }

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<HealthAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = false, // DS-14
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent)
            return;

        var msg = Loc.GetString("health-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popupSystem.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<HealthAnalyzerComponent> uid, ref HealthAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        OpenUserInterface(args.User, uid);
        BeginAnalyzingEntity(uid, args.Target.Value);
        args.Handled = true;
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<HealthAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<HealthAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } patient)
            StopAnalyzingEntity(ent, patient);
    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<HealthAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, HealthAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, HealthAnalyzerUiKey.Key, user);
    }

    /// <summary>
    /// Mark the entity as having its health analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that should receive the updates</param>
    /// <param name="target">The entity to start analyzing</param>
    private void BeginAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        //Link the health analyzer to the scanned entity
        healthAnalyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(healthAnalyzer.Owner);

        UpdateScannedUser(healthAnalyzer, target, true);
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void StopAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        //Unlink the analyzer
        healthAnalyzer.Comp.ScannedEntity = null;

        _toggle.TryDeactivate(healthAnalyzer.Owner);

        UpdateScannedUser(healthAnalyzer, target, false);
    }

    /// <summary>
    /// If the scanner is active, sends one last update and sets it to inactive.
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void PauseAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        if (!healthAnalyzer.Comp.IsAnalyzerActive)
            return;

        UpdateScannedUser(healthAnalyzer, target, false);
        healthAnalyzer.Comp.IsAnalyzerActive = false;
    }

    /// <summary>
    /// Send an update for the target to the healthAnalyzer
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer</param>
    /// <param name="target">The entity being scanned</param>
    /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
    public void UpdateScannedUser(EntityUid healthAnalyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(healthAnalyzer, HealthAnalyzerUiKey.Key)
            || !HasComp<DamageableComponent>(target))
            return;

        var uiState = GetHealthAnalyzerUiState(target);
        uiState.ScanMode = scanMode;

        //DS14-start
        if (TryComp<HealthAnalyzerComponent>(healthAnalyzer, out var analyzerComp))
        {
            Dictionary<string, FixedPoint2>? damageDict = null;
            FixedPoint2 totalDamage = FixedPoint2.Zero;

            if (TryComp<DamageableComponent>(target, out var damageable))
            {
                damageDict = new Dictionary<string, FixedPoint2>(damageable.Damage.DamageDict);
                totalDamage = damageable.TotalDamage;
            }

            analyzerComp.LastScanData = new HealthAnalyzerData(
                Name(target),
                damageDict,
                totalDamage,
                uiState.Reagents
            );
        }
        //DS14-end

        _uiSystem.ServerSendUiMessage(
            healthAnalyzer,
            HealthAnalyzerUiKey.Key,
            new HealthAnalyzerScannedUserMessage(uiState)
        );
    }

    /// <summary>
    /// Creates a HealthAnalyzerState based on the current state of an entity.
    /// </summary>
    /// <param name="target">The entity being scanned</param>
    /// <returns></returns>
    public HealthAnalyzerUiState GetHealthAnalyzerUiState(EntityUid? target)
    {
        if (!target.HasValue || !HasComp<DamageableComponent>(target))
            return new HealthAnalyzerUiState();

        var entity = target.Value;
        var bodyTemperature = float.NaN;

        if (TryComp<TemperatureComponent>(entity, out var temp))
            bodyTemperature = temp.CurrentTemperature;

        var bloodAmount = float.NaN;
        var bleeding = false;
        var unrevivable = false;
        var unclonable = false; // DS14
        var reagents = new List<HealthAnalyzerReagentEntry>(); // DS14

        if (TryComp<BloodstreamComponent>(entity, out var bloodstream) &&
            _solutionContainerSystem.ResolveSolution(entity, bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution, out var bloodSolution))
        {
            bloodAmount = _bloodstreamSystem.GetBloodLevel(entity);
            bleeding = bloodstream.BleedAmount > 0;
            reagents = GetInjectedReagents(bloodSolution, bloodstream); // DS14
        }

        if (TryComp<UnrevivableComponent>(entity, out var unrevivableComp) && unrevivableComp.Analyzable)
            unrevivable = true;

        // DS14-start
        if (HasComp<UncloningComponent>(entity))
            unclonable = true;
        // DS14-end

        return new HealthAnalyzerUiState(
                    GetNetEntity(entity),
                    bodyTemperature,
                    bloodAmount,
                    null,
                    bleeding,
                    unrevivable,
                    unclonable, // DS14
                    reagents // DS14
                );
    }

    // DS14-start
    private List<HealthAnalyzerReagentEntry> GetInjectedReagents(Solution bloodSolution, BloodstreamComponent bloodstream)
    {
        var result = new List<HealthAnalyzerReagentEntry>();

        foreach (var (reagentId, quantity) in bloodSolution)
        {
            if (quantity <= FixedPoint2.Zero || IsBloodReagent(reagentId.Prototype, bloodstream))
                continue;

            result.Add(new HealthAnalyzerReagentEntry(
                reagentId.Prototype,
                quantity,
                IsOverdose(reagentId.Prototype, quantity)));
        }

        result.Sort((left, right) => right.Quantity.CompareTo(left.Quantity));
        return result;
    }

    private bool IsBloodReagent(string reagentId, BloodstreamComponent bloodstream)
    {
        foreach (var (bloodReagentId, _) in bloodstream.BloodReferenceSolution)
        {
            if (bloodReagentId.Prototype == reagentId)
                return true;
        }

        return false;
    }

    private bool IsOverdose(string reagentId, FixedPoint2 quantity)
    {
        if (!_prototype.TryIndex<ReagentPrototype>(reagentId, out var reagent) || reagent.Metabolisms == null)
            return false;

        FixedPoint2? overdoseThreshold = null;

        foreach (var metabolism in reagent.Metabolisms.Values)
        {
            foreach (var effect in metabolism.Effects)
            {
                if (effect.Conditions == null)
                    continue;

                if (effect is not HealthChange healthChange || !HasPositiveDamage(healthChange.Damage))
                    continue;

                foreach (var condition in effect.Conditions)
                {
                    if (condition is not ReagentCondition reagentCondition || reagentCondition.Reagent != reagentId || reagentCondition.Min <= FixedPoint2.Zero)
                        continue;

                    if (overdoseThreshold == null || reagentCondition.Min < overdoseThreshold.Value)
                        overdoseThreshold = reagentCondition.Min;
                }
            }
        }

        return overdoseThreshold != null && quantity >= overdoseThreshold.Value;
    }

    private bool HasPositiveDamage(Content.Shared.Damage.DamageSpecifier damage)
    {
        foreach (var (_, amount) in damage.DamageDict)
        {
            if (amount > FixedPoint2.Zero)
                return true;
        }

        return false;
    }
    private void OnPrint(Entity<HealthAnalyzerComponent> entity, ref HealthAnalyzerPrintUiMessage args)
    {
        if (_timing.CurTime < entity.Comp.NextPrintAllowed)
            return;

        if (entity.Comp.LastScanData is not { } data)
            return;

        entity.Comp.NextPrintAllowed = _timing.CurTime + entity.Comp.PrintDelay;



        var sb = new StringBuilder();
        sb.AppendLine($"[bold]Пациент:[/bold] {data.PatientName}");
        sb.AppendLine($"[bold]Время смены:[/bold] {_timing.CurTime.ToString(@"hh\:mm\:ss")}");
        sb.AppendLine("-----------------------------------");
        sb.AppendLine("[bold]Повреждения:[/bold]");
        sb.AppendLine($"[bold]Общие повреждения:[/bold] [color=darkred]{data.TotalDamage}[/color]");
        if (data.DamageDict == null || data.DamageDict.Count == 0 || data.TotalDamage == 0)
        {
            sb.AppendLine("[color=green]• Повреждений не обнаружено[/color]");
        }
        else
        {
            var processedTypes = new HashSet<string>();

            foreach (var groupProto in _prototype.EnumeratePrototypes<DamageGroupPrototype>())
            {
                FixedPoint2 groupTotal = FixedPoint2.Zero;
                var subTypes = new List<(string TypeName, FixedPoint2 Amount)>();

                foreach (var typeId in groupProto.DamageTypes)
                {
                    var typeIdStr = typeId.ToString();

                    if (data.DamageDict.TryGetValue(typeIdStr, out var amount) && amount > 0)
                    {
                        groupTotal += amount;

                        var locTypeKey = $"damage-type-{typeIdStr.ToLowerInvariant()}";
                        var typeName = Loc.HasString(locTypeKey) ? Loc.GetString(locTypeKey) : typeIdStr;

                        subTypes.Add((typeName, amount));
                        processedTypes.Add(typeIdStr);
                    }
                }

                if (groupTotal > 0)
                {
                    var groupIdStr = groupProto.ID.ToString();
                    var groupLocKey = $"damage-group-{groupIdStr.ToLowerInvariant()}";
                    var groupName = Loc.HasString(groupLocKey) ? Loc.GetString(groupLocKey) : groupProto.LocalizedName;

                    sb.AppendLine($"[bold]• {groupName}:[/bold] [color=red]{groupTotal}[/color]");

                    foreach (var (typeName, amount) in subTypes)
                    {
                        sb.AppendLine($"  - {typeName}: {amount}");
                    }
                }
            }

            foreach (var (type, amount) in data.DamageDict)
            {
                if (amount <= 0 || processedTypes.Contains(type))
                    continue;

                var locKey = $"damage-type-{type.ToLowerInvariant()}";
                var typeName = Loc.HasString(locKey) ? Loc.GetString(locKey) : type;

                sb.AppendLine($"• {typeName}: [color=red]{amount}[/color]");
            }
        }

        if (data.Reagents != null && data.Reagents.Count > 0)
        {
            sb.AppendLine("-----------------------------------");
            sb.AppendLine("[bold]Реагенты в крови:[/bold]");
            foreach (var entry in data.Reagents)
            {
                var reagentName = entry.ReagentId;
                if (_prototype.TryIndex<ReagentPrototype>(entry.ReagentId, out var reagentProto))
                {
                    reagentName = reagentProto.LocalizedName;
                }

                sb.AppendLine($"• {reagentName}: {entry.Quantity}u");
            }
        }

        var paperUid = Spawn("PaperMedicalItem", _transformSystem.GetMapCoordinates(entity.Owner));

        _audio.PlayPvs(entity.Comp.PrintSound, entity);

        _paperSystem.SetContent(paperUid, sb.ToString());
        _metaData.SetEntityName(paperUid, $"медицинский отчет ({data.PatientName})");
        _handsSystem.TryPickupAnyHand(args.Actor, paperUid);
    }
    //DS14-end
}