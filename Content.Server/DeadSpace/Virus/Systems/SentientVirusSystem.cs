// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus;
using Robust.Server.GameObjects;
using Content.Shared.DeadSpace.Virus.Components;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Content.Shared.Body.Prototypes;
using Content.Shared.Actions;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server.DeadSpace.Virus.Systems;

public sealed class SentientVirusSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly VirusSystem _virusSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    private const int PrimaryPacientPrice = 250;
    private const int ModifyPointsRegenPerInfected = 5;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SentientVirusComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SentientVirusComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SentientVirusComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SentientVirusComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SentientVirusComponent, ShopMutationActionEvent>(OnShopMutation);
        SubscribeLocalEvent<SentientVirusComponent, SelectPrimaryPatientEvent>(OnSelectPrimaryPatient);
        SubscribeLocalEvent<SentientVirusComponent, TeleportToPrimaryPatientEvent>(OnTeleportToPrimaryPatient);
        SubscribeLocalEvent<SentientVirusComponent, EvolutionConsoleUiButtonPressedMessage>(OnButtonPressed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SentientVirusComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timedWindowSystem.IsExpired(component.UpdateWindow))
            {
                _timedWindowSystem.Reset(component.UpdateWindow);
                UpdateSentientVirus(uid, component);
            }
        }
    }

    private void OnTeleportToPrimaryPatient(EntityUid uid, SentientVirusComponent component, TeleportToPrimaryPatientEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var list = component.CurrentPrimaryInfected;

        if (list.Count <= 0)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("sentient-virus-teleport-no-primary-infected"),
                uid,
                uid,
                PopupType.Medium
            );
            return;
        }

        if (component.SelectedPrimaryInfected >= list.Count)
            component.SelectedPrimaryInfected = 0;

        var target = list[component.SelectedPrimaryInfected];
        var entityCoords = Transform(target).Coordinates;

        _transform.SetCoordinates(uid, entityCoords);
        _transform.AttachToGridOrMap(uid);

        component.SelectedPrimaryInfected++;
    }

    private void OnSelectPrimaryPatient(EntityUid uid, SentientVirusComponent component, SelectPrimaryPatientEvent args)
    {
        if (args.Target == uid)
            return;

        if (component.Data == null)
            return;

        args.Handled = true;

        if (TryComp<VirusComponent>(args.Target, out var virus)
            && virus.Data.StrainId == component.Data.StrainId)
        {
            AddPrimaryPatient(uid, args.Target, component);
        }
        else if (!_virusSystem.CanInfect(args.Target, component.Data))
        {
            _popupSystem.PopupEntity(
                    Loc.GetString("sentient-virus-infect-impossible-target"),
                    uid,
                    uid,
                    PopupType.Medium);

            return;
        }

        AddPrimaryPatient(uid, args.Target, component);
    }

    private void AddPrimaryPatient(EntityUid uid, EntityUid target, SentientVirusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Data == null)
            return;

        var totalPrice = PrimaryPacientPrice * component.FactPrimaryInfected;
        var missingPoints2 = totalPrice - component.Data.MutationPoints;

        if (component.Data.MutationPoints < totalPrice)
        {
            _popupSystem.PopupEntity(
                Loc.GetString("sentient-virus-infect-no-points", ("price", missingPoints2)),
                uid,
                uid,
                PopupType.Medium
            );
            return;
        }

        if (TryAddPrimaryInfected(uid, target, component))
            component.Data.MutationPoints -= totalPrice;
        else
            _popupSystem.PopupEntity(Loc.GetString("sentient-virus-infect-failed-source"), uid, uid, PopupType.Medium);
    }

    private void UpdateSentientVirus(EntityUid uid, SentientVirusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Data == null)
            return;

        component.Data.MutationPoints += component.Data.RegenMutationPoints + _virusSystem.GetInfectedCount(component.Data.StrainId) * ModifyPointsRegenPerInfected;
    }

    private void OnButtonPressed(EntityUid uid, SentientVirusComponent component, EvolutionConsoleUiButtonPressedMessage args)
    {
        switch (args.Button)
        {
            case EvolutionConsoleUiButton.EvolutionSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<VirusSymptomPrototype>(args.Symptom, out var proto)
                        || component.Data == null)
                        return;

                    if (!VirusSystem.CanAddSymptom(
                            component.Data.ActiveSymptom,
                            args.Symptom,
                            proto,
                            isTaipan: false))
                        return;

                    var price = _virusSystem.GetSymptomPrice(component.Data, proto);
                    if (component.Data.MutationPoints < price)
                        return;

                    if (proto.RequiredSymptom != null)
                    {
                        component.Data.ActiveSymptom.Remove(proto.RequiredSymptom.Value);
                        var oldInstance = _virusSystem.CreateSymptomInstance(proto.RequiredSymptom.Value);
                        oldInstance.ApplyDataEffect(component.Data, false);
                    }

                    component.Data.MutationPoints -= price;
                    component.Data.ActiveSymptom.Add(args.Symptom);

                    var symptomInstance = _virusSystem.CreateSymptomInstance(proto);
                    symptomInstance.ApplyDataEffect(component.Data, add: true);

                    UpdateVirusDataForStrain(uid, component);

                    break;
                }
            case EvolutionConsoleUiButton.EvolutionBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || component.Data == null)
                        return;

                    var price = _virusSystem.GetBodyPrice(component.Data);
                    if (component.Data.MutationPoints < price)
                        return;

                    component.Data.MutationPoints -= price;
                    component.Data.BodyWhitelist.Add(args.Body);
                    UpdateVirusDataForStrain(uid, component);
                    break;
                }
            case EvolutionConsoleUiButton.DeleteSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<VirusSymptomPrototype>(args.Symptom, out var proto)
                        || component.Data == null)
                        return;

                    var price = _virusSystem.GetSymptomDeletePrice(component.Data.MultiPriceDeleteSymptom);
                    if (component.Data.MutationPoints < price)
                        return;

                    component.Data.MutationPoints -= price;
                    component.Data.ActiveSymptom.Remove(args.Symptom);

                    var symptomInstance = _virusSystem.CreateSymptomInstance(proto);
                    symptomInstance.ApplyDataEffect(component.Data, add: false);

                    UpdateVirusDataForStrain(uid, component);

                    break;
                }
            case EvolutionConsoleUiButton.DeleteBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || component.Data == null)
                        return;

                    var price = _virusSystem.GetBodyDeletePrice();
                    if (component.Data.MutationPoints < price)
                        return;

                    component.Data.MutationPoints -= price;
                    component.Data.BodyWhitelist.Remove(args.Body);
                    UpdateVirusDataForStrain(uid, component);
                    break;
                }
            default:
                break;
        }

        UpdateUserInterface((uid, component));
    }

    /// <summary>
    ///     Обновляет данные всех VirusComponent с данным StrainId.
    /// </summary>
    public void UpdateVirusDataForStrain(EntityUid uid, SentientVirusComponent? source = null)
    {
        if (!Resolve(uid, ref source))
            return;

        if (source.Data == null)
            return;

        if (string.IsNullOrEmpty(source.Data.StrainId) || source.Data == null)
            return;

        var query = EntityQueryEnumerator<VirusComponent>();
        while (query.MoveNext(out var virusUid, out var virusComponent))
        {
            if (virusComponent.Data != null && virusComponent.Data.StrainId == source.Data.StrainId)
            {
                virusComponent.Data.ApplyInfectionData(source.Data);
                _virusSystem.RefreshSymptoms((virusUid, virusComponent));
            }
        }
    }

    public bool TryAddPrimaryInfected(EntityUid uid, EntityUid target, SentientVirusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.FactPrimaryInfected >= component.MaxPrimaryInfected)
            return false;

        if (HasComp<PrimaryPacientComponent>(target))
            return false;

        if (HasComp<VirusComponent>(target))
            return false;

        if (component.Data == null)
            return false;

        _virusSystem.InfectEntity(component.Data, target);

        component.CurrentPrimaryInfected.Add(target);
        component.FactPrimaryInfected++;

        var primary = new PrimaryPacientComponent(uid, component.Data.StrainId);

        AddComp(target, primary);

        return true;
    }

    public void RemovePrimaryInfected(EntityUid uid, EntityUid target, SentientVirusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.CurrentPrimaryInfected.Remove(target);

        if (component.CurrentPrimaryInfected.Count <= 0)
            QueueDel(uid);
    }

    private void OnShopMutation(Entity<SentientVirusComponent> entity, ref ShopMutationActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<UserInterfaceComponent>(entity, out var userInterface))
            return;

        UpdateUserInterface((entity, entity.Comp));
        _uiSystem.OpenUi((entity, userInterface), VirusEvolutionConsoleUiKey.Key, entity);
    }

    private void OnInit(Entity<SentientVirusComponent> entity, ref ComponentInit args)
    {
        var strain = _virusSystem.GenerateStrainId();

        if (entity.Comp.Data == null)
            entity.Comp.Data = new VirusData(strain);
        else
            entity.Comp.Data.StrainId = strain;

        _timedWindowSystem.Reset(entity.Comp.UpdateWindow);

        EnsureVirusActions(entity);
    }

    private void OnMindAdded(EntityUid uid, SentientVirusComponent component, MindAddedMessage args)
    {
        EnsureVirusActions((uid, component));
    }

    private void OnPlayerAttached(EntityUid uid, SentientVirusComponent component, PlayerAttachedEvent args)
    {
        EnsureVirusActions((uid, component));
    }

    private void EnsureVirusActions(Entity<SentientVirusComponent> entity)
    {
        _actionsSystem.AddAction(entity, ref entity.Comp.ShopMutationActionEntity, entity.Comp.ShopMutationAbility, entity);
        _actionsSystem.AddAction(entity, ref entity.Comp.SelectPrimaryPatientActionEntity, entity.Comp.SelectPrimaryPatientAbility, entity);
        _actionsSystem.AddAction(entity, ref entity.Comp.TeleportToPrimaryPatientActionEntity, entity.Comp.TeleportToPrimaryPatientAbility, entity);
    }

    private void OnShutdown(Entity<SentientVirusComponent> entity, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(entity.Owner, entity.Comp.ShopMutationActionEntity);
        _actionsSystem.RemoveAction(entity.Owner, entity.Comp.SelectPrimaryPatientActionEntity);
        _actionsSystem.RemoveAction(entity.Owner, entity.Comp.TeleportToPrimaryPatientActionEntity);
    }

    public void UpdateUserInterface(Entity<SentientVirusComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryComp<UserInterfaceComponent>(entity, out var userInterface))
            return;

        if (!_uiSystem.HasUi(entity, VirusEvolutionConsoleUiKey.Key, userInterface))
            return;

        var newState = GetUserInterfaceState((entity, entity.Comp));
        _uiSystem.SetUiState((entity, userInterface), VirusEvolutionConsoleUiKey.Key, newState);
    }

    private VirusEvolutionConsoleBoundUserInterfaceState GetUserInterfaceState(Entity<SentientVirusComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return default!;

        var data = console.Comp.Data;
        var infectivity = 0f;
        var infectedCount = data != null ? _virusSystem.GetInfectedCount(data.StrainId) : 0;
        var pointsPerSecond = data != null ? data.RegenMutationPoints + infectedCount * ModifyPointsRegenPerInfected : 0;

        if (data != null)
        {
            foreach (var sympId in data.ActiveSymptom)
            {
                if (_prototypeManager.TryIndex(sympId, out var prototype))
                    infectivity += prototype.AddInfectivity;
            }
        }

        return new VirusEvolutionConsoleBoundUserInterfaceState(
            data?.MutationPoints ?? 0,
            data?.MultiPriceDeleteSymptom ?? 0,
            true,
            true,
            true,
            true,
            data != null,
            data?.ActiveSymptom,
            data?.BodyWhitelist,
            data?.MaxThreshold ?? 100f,
            infectivity,
            infectedCount,
            pointsPerSecond,
            isSentientVirus: true,
            isTaipan: false
        );
    }
}
