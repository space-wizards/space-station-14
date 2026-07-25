// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Content.Shared.DeadSpace.Virus;
using Content.Server.DeadSpace.Virus.Components;
using Content.Shared.DeadSpace.Virus.Components;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Content.Shared.Body.Prototypes;

namespace Content.Server.DeadSpace.Virus.Systems;

public sealed class VirusEvolutionConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly VirusSolutionAnalyzerSystem _virusSolutionAnalyzer = default!;
    [Dependency] private readonly VirusSystem _virusSystem = default!;
    [Dependency] private readonly VirusDiagnoserDataServerSystem _dataServer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusEvolutionConsoleComponent, EvolutionConsoleUiButtonPressedMessage>(OnButtonPressed);
        SubscribeLocalEvent<VirusEvolutionConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<VirusEvolutionConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<VirusEvolutionConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VirusEvolutionConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<VirusEvolutionConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<VirusEvolutionConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnButtonPressed(EntityUid uid, VirusEvolutionConsoleComponent component, EvolutionConsoleUiButtonPressedMessage args)
    {
        if (!_powerReceiverSystem.IsPowered(uid))
            return;

        RecheckConnections((uid, component));

        if (!component.DataServerInRange || !component.SolutionAnalyzerInRange)
            return;

        if (component.VirusSolutionAnalyzer == null)
            return;

        if (!TryComp<VirusSolutionAnalyzerComponent>(component.VirusSolutionAnalyzer, out var analyzer))
            return;

        if (analyzer.Status != VirusSolutionAnalyzerStatus.On)
            return;

        if (component.VirusDiagnoserDataServer == null
            || !TryComp<VirusDiagnoserDataServerComponent>(component.VirusDiagnoserDataServer, out var server))
            return;

        VirusData? virusData = null;

        if (_virusSolutionAnalyzer.TryGetVirusDataFromContainer(
                component.VirusSolutionAnalyzer.Value,
                out var virusDataList)
            && virusDataList != null)
        {
            var source = virusDataList.FirstOrDefault();
            // Скорее можно обойтись и без копии, но мне не хочется это проверять, ибо я уже обжигался
            virusData = source != null
                ? (VirusData)source.Clone()
                : null;
        }

        var mutated = false;

        switch (args.Button)
        {
            case EvolutionConsoleUiButton.EvolutionSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<VirusSymptomPrototype>(args.Symptom, out var symptomProto)
                        || virusData == null)
                        return;

                    if (!VirusSystem.CanAddSymptom(
                            virusData.ActiveSymptom,
                            args.Symptom,
                            symptomProto,
                            component.IsTaipan))
                        return;

                    var price = _virusSystem.GetSymptomPrice(virusData, args.Symptom);
                    if (server.Points < price)
                        return;

                    if (symptomProto.RequiredSymptom != null)
                        _virusSolutionAnalyzer.RemSymptom((component.VirusSolutionAnalyzer.Value, analyzer), symptomProto.RequiredSymptom.Value);

                    if (!_virusSolutionAnalyzer.AddSymptom((component.VirusSolutionAnalyzer.Value, analyzer), args.Symptom))
                        return;

                    server.Points -= price;
                    mutated = true;
                    break;
                }
            case EvolutionConsoleUiButton.EvolutionBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || virusData == null)
                        return;

                    if (virusData.BodyWhitelist.Contains(args.Body))
                        return;

                    var price = _virusSystem.GetBodyPrice(virusData);
                    if (server.Points < price)
                        return;

                    if (!_virusSolutionAnalyzer.AddBody((component.VirusSolutionAnalyzer.Value, analyzer), args.Body))
                        return;

                    server.Points -= price;
                    mutated = true;
                    break;
                }
            case EvolutionConsoleUiButton.DeleteSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<VirusSymptomPrototype>(args.Symptom, out _)
                        || virusData == null)
                        return;

                    if (!virusData.ActiveSymptom.Contains(args.Symptom))
                        return;

                    var price = _virusSystem.GetSymptomDeletePrice(virusData.MultiPriceDeleteSymptom);
                    if (server.Points < price)
                        return;

                    if (!_virusSolutionAnalyzer.RemSymptom((component.VirusSolutionAnalyzer.Value, analyzer), args.Symptom))
                        return;

                    server.Points -= price;
                    mutated = true;
                    break;
                }
            case EvolutionConsoleUiButton.DeleteBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || virusData == null)
                        return;

                    if (!virusData.BodyWhitelist.Contains(args.Body))
                        return;

                    var price = _virusSystem.GetBodyDeletePrice();
                    if (server.Points < price)
                        return;

                    if (!_virusSolutionAnalyzer.RemBody((component.VirusSolutionAnalyzer.Value, analyzer), args.Body))
                        return;

                    server.Points -= price;
                    mutated = true;
                    break;
                }
            default:
                break;
        }

        if (mutated)
            _dataServer.UpdateConnectedInterfaces(component.VirusDiagnoserDataServer.Value, server);
    }

    private void OnPowerChanged(EntityUid uid, VirusEvolutionConsoleComponent component, ref PowerChangedEvent args)
    {
        RecheckConnections((uid, component));
    }

    private void OnMapInit(EntityUid uid, VirusEvolutionConsoleComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
            return;

        foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
        {
            if (TryComp<VirusSolutionAnalyzerComponent>(port, out var solutionAnalyzer))
            {
                component.VirusSolutionAnalyzer = port;
                solutionAnalyzer.ConnectedEvolutionConsole = uid;
            }

            if (TryComp<VirusDiagnoserDataServerComponent>(port, out var server))
            {
                component.VirusDiagnoserDataServer = port;
                server.ConnectedEvolutionConsole = uid;
            }
        }
    }

    private void OnNewLink(EntityUid uid, VirusEvolutionConsoleComponent component, NewLinkEvent args)
    {
        if (TryComp<VirusDiagnoserDataServerComponent>(args.Sink, out var server) && args.SourcePort == component.VirusDiagnoserDataServerPort)
        {
            component.VirusDiagnoserDataServer = args.Sink;
            server.ConnectedEvolutionConsole = uid;
        }

        if (TryComp<VirusSolutionAnalyzerComponent>(args.Sink, out var solutionAnalyzer) && args.SourcePort == component.VirusSolutionAnalyzerPort)
        {
            component.VirusSolutionAnalyzer = args.Sink;
            solutionAnalyzer.ConnectedEvolutionConsole = uid;
        }

        RecheckConnections((uid, component));
    }

    private void OnPortDisconnected(Entity<VirusEvolutionConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port == ent.Comp.VirusSolutionAnalyzerPort)
            ent.Comp.VirusSolutionAnalyzer = null;

        if (args.Port == ent.Comp.VirusDiagnoserDataServerPort)
            ent.Comp.VirusDiagnoserDataServer = null;

        UpdateUserInterface((ent, ent.Comp));
    }

    private void OnUIOpen(EntityUid uid, VirusEvolutionConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        RecheckConnections((uid, component));
    }

    private void OnAnchorChanged(EntityUid uid, VirusEvolutionConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            RecheckConnections((uid, component));
            return;
        }

        RecheckConnections((uid, component));
    }

    public void UpdateUserInterface(Entity<VirusEvolutionConsoleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryComp<UserInterfaceComponent>(entity, out var userInterface))
            return;

        if (!_uiSystem.HasUi(entity, VirusEvolutionConsoleUiKey.Key, userInterface))
            return;

        if (!_powerReceiverSystem.IsPowered(entity))
        {
            _uiSystem.CloseUis((entity, userInterface));
            return;
        }

        var newState = GetUserInterfaceState((entity, entity.Comp));
        _uiSystem.SetUiState((entity, userInterface), VirusEvolutionConsoleUiKey.Key, newState);
    }

    public void RecheckConnections(Entity<VirusEvolutionConsoleComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        console.Comp.DataServerInRange = false;
        console.Comp.SolutionAnalyzerInRange = false;

        var distance = 0f;
        if (console.Comp.VirusDiagnoserDataServer != null)
        {
            console.Comp.DataServerInRange =
                Transform(console.Comp.VirusDiagnoserDataServer.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out distance) &&
                distance <= console.Comp.MaxDistanceForDataServer;
        }
        if (console.Comp.VirusSolutionAnalyzer != null)
        {
            console.Comp.SolutionAnalyzerInRange =
                Transform(console.Comp.VirusSolutionAnalyzer.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out distance) &&
                distance <= console.Comp.MaxDistanceForOther;
        }

        UpdateUserInterface((console, console.Comp));
    }

    private VirusEvolutionConsoleBoundUserInterfaceState GetUserInterfaceState(Entity<VirusEvolutionConsoleComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return default!;

        VirusData? virusData = null;

        int points = 0;

        var dataServerConnected = console.Comp.VirusDiagnoserDataServer != null;
        var solutionAnalyzerConnected = console.Comp.VirusSolutionAnalyzer != null;
        var solutionAnalyzerStatus = VirusSolutionAnalyzerStatus.Off;

        if (console.Comp.VirusSolutionAnalyzer != null &&
            TryComp<VirusSolutionAnalyzerComponent>(console.Comp.VirusSolutionAnalyzer.Value, out var analyzer))
        {
            solutionAnalyzerStatus = analyzer.Status;
        }

        if (console.Comp.VirusSolutionAnalyzer != null &&
            _virusSolutionAnalyzer.TryGetVirusDataFromContainer(console.Comp.VirusSolutionAnalyzer.Value, out var virusDataList))
        {
            var source = virusDataList.FirstOrDefault();
            // Скорее можно обойтись и без копии, но мне не хочется это проверять, ибо я уже обжигался
            virusData = source != null
                ? (VirusData)source.Clone()
                : null;
        }

        if (console.Comp.VirusDiagnoserDataServer != null &&
            TryComp<VirusDiagnoserDataServerComponent>(console.Comp.VirusDiagnoserDataServer.Value, out var server))
        {
            points = server.Points;
        }

        return new VirusEvolutionConsoleBoundUserInterfaceState(
            points,
            virusData?.MultiPriceDeleteSymptom ?? 0,
            dataServerConnected,
            solutionAnalyzerConnected,
            console.Comp.DataServerInRange,
            console.Comp.SolutionAnalyzerInRange,
            virusData != null,
            virusData?.ActiveSymptom,
            virusData?.BodyWhitelist,
            isSentientVirus: false,
            solutionAnalyzerStatus: solutionAnalyzerStatus,
            isTaipan: console.Comp.IsTaipan
        );
    }


}

