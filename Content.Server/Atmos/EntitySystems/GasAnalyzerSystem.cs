using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.NodeContainer;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using static Content.Shared.Atmos.Components.GasAnalyzerComponent;

namespace Content.Server.Atmos.EntitySystems;

[UsedImplicitly]
public sealed class GasAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AtmosphereSystem _atmo = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    /// <summary>
    /// Minimum moles of a gas to be sent to the client.
    /// </summary>
    private const float UIMinMoles = 0.01f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);

        Subs.BuiEvents<GasAnalyzerComponent>(GasAnalyzerUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
        });
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveGasAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var analyzer))
        {
            // Don't update every tick
            analyzer.AccumulatedFrametime += frameTime;

            if (analyzer.AccumulatedFrametime < analyzer.UpdateInterval)
                continue;

            analyzer.AccumulatedFrametime -= analyzer.UpdateInterval;

            if (!UpdateAnalyzer(uid))
                RemCompDeferred<ActiveGasAnalyzerComponent>(uid);
        }
    }

    /// <summary>
    /// Activates the analyzer when used in the world, scanning the target entity (if it exists) and the tile the analyzer is in
    /// </summary>
    private void OnAfterInteract(Entity<GasAnalyzerComponent> entity, ref AfterInteractEvent args)
    {
        var target = args.Target;
        if (target != null && !_interactionSystem.InRangeUnobstructed((args.User, null), (target.Value, null)))
        {
            target = null; // if the target is out of reach, invalidate it
        }
        // always run the analyzer, regardless of weather or not there is a target
        // since we can always show the local environment.
        ActivateAnalyzer(entity, args.User, target);
        args.Handled = true;
    }

    /// <summary>
    /// Handles analyzer activation logic
    /// </summary>
    private void ActivateAnalyzer(Entity<GasAnalyzerComponent> entity, EntityUid user, EntityUid? target = null)
    {
        if (!_userInterface.TryOpenUi(entity.Owner, GasAnalyzerUiKey.Key, user))
            return;

        entity.Comp.Target = target;
        entity.Comp.User = user;
        entity.Comp.Enabled = true;
        Dirty(entity);
        _appearance.SetData(entity.Owner, GasAnalyzerVisuals.Enabled, entity.Comp.Enabled);
        EnsureComp<ActiveGasAnalyzerComponent>(entity.Owner);
        UpdateAnalyzer(entity.Owner, entity.Comp);
    }

    /// <summary>
    /// Closes the UI, sets the icon to off, and removes it from the update list
    /// </summary>
    private void DisableAnalyzer(Entity<GasAnalyzerComponent> entity, EntityUid? user = null)
    {
        _userInterface.CloseUi(entity.Owner, GasAnalyzerUiKey.Key, user);

        if (user.HasValue && entity.Comp.Enabled)
            _popup.PopupEntity(Loc.GetString("gas-analyzer-shutoff"), user.Value, user.Value);

        entity.Comp.Enabled = false;
        Dirty(entity);
        _appearance.SetData(entity.Owner, GasAnalyzerVisuals.Enabled, entity.Comp.Enabled);
        RemCompDeferred<ActiveGasAnalyzerComponent>(entity.Owner);
    }

    /// <summary>
    /// Disables the analyzer when the user closes the UI
    /// </summary>
    private void OnBoundUIClosed(Entity<GasAnalyzerComponent> entity, ref BoundUIClosedEvent args)
    {
        if (HasComp<ActiveGasAnalyzerComponent>(entity.Owner)
            && !_userInterface.IsUiOpen(entity.Owner, args.UiKey))
        {
            DisableAnalyzer(entity, args.Actor);
        }
    }

    /// <summary>
    /// Enables the analyzer when the user opens the UI
    /// </summary>
    private void OnBoundUIOpened(Entity<GasAnalyzerComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (!HasComp<ActiveGasAnalyzerComponent>(entity.Owner)
            && _userInterface.IsUiOpen(entity.Owner, args.UiKey))
        {
            ActivateAnalyzer(entity, args.Actor);
        }
    }

    /// <summary>
    /// Fetches fresh data for the analyzer. Should only be called by Update or when the user requests an update via refresh button
    /// </summary>
    private bool UpdateAnalyzer(EntityUid uid, GasAnalyzerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // check if the user has walked away from what they scanned
        if (component.Target.HasValue)
        {
            // Listen! Even if you don't want the Gas Analyzer to work on moving targets, you should use
            // this code to determine if the object is still generally in range so that the check is consistent with the code
            // in OnAfterInteract() and also consistent with interaction code in general.
            if (!_interactionSystem.InRangeUnobstructed((component.User, null), (component.Target.Value, null)))
            {
                if (component.User is { } userId && component.Enabled)
                    _popup.PopupEntity(Loc.GetString("gas-analyzer-object-out-of-range"), userId, userId);

                component.Target = null;
            }
        }

        var gasMixList = new List<GasMixEntry>();

        // Fetch the environmental atmosphere around the scanner. This must be the first entry
        var tileMixture = _atmo.GetContainingMixture(uid, true);
        if (tileMixture != null)
        {
            gasMixList.Add(new GasMixEntry(Loc.GetString("gas-analyzer-window-environment-tab-label"), tileMixture.Volume, tileMixture.Pressure, tileMixture.Temperature,
                GenerateGasEntryArray(tileMixture)));
        }
        else
        {
            // No gases were found
            gasMixList.Add(new GasMixEntry(Loc.GetString("gas-analyzer-window-environment-tab-label"), 0f, 0f, 0f));
        }

        var deviceFlipped = false;
        if (component.Target != null)
        {
            if (Deleted(component.Target))
            {
                component.Target = null;
                DisableAnalyzer((uid, component), component.User);
                return false;
            }

            var validTarget = false;

            // gas analyzed was used on an entity, try to request gas data via event for override
            var ev = new GasAnalyzerScanEvent();
            RaiseLocalEvent(component.Target.Value, ev);

            if (ev.GasMixtures != null)
            {
                foreach (var mixes in ev.GasMixtures)
                {
                    if (mixes.Item2 != null)
                    {
                        gasMixList.Add(new GasMixEntry(mixes.Item1, mixes.Item2.Volume, mixes.Item2.Pressure, mixes.Item2.Temperature, GenerateGasEntryArray(mixes.Item2)));
                        validTarget = true;
                    }
                }

                deviceFlipped = ev.DeviceFlipped;
            }
            else
            {
                // No override, fetch manually, to handle flippable devices you must subscribe to GasAnalyzerScanEvent
                if (TryComp(component.Target, out NodeContainerComponent? node))
                {
                    foreach (var pair in node.Nodes)
                    {
                        if (pair.Value is PipeNode pipeNode)
                        {
                            // check if the volume is zero for some reason so we don't divide by zero
                            if (pipeNode.Air.Volume == 0f)
                                continue;
                            // only display the gas in the analyzed pipe element, not the whole system
                            var pipeAir = pipeNode.Air.Clone();
                            pipeAir.Multiply(pipeNode.Volume / pipeNode.Air.Volume);
                            pipeAir.Volume = pipeNode.Volume;
                            gasMixList.Add(new GasMixEntry(pair.Key, pipeAir.Volume, pipeAir.Pressure, pipeAir.Temperature, GenerateGasEntryArray(pipeAir)));
                            validTarget = true;
                        }
                    }
                }
            }

            // If the target doesn't actually have any gas mixes to add,
            // invalidate it as the target
            if (!validTarget)
            {
                component.Target = null;
            }
        }

        // Don't bother sending a UI message with no content, and stop updating I guess?
        if (gasMixList.Count == 0)
            return false;

        _userInterface.ServerSendUiMessage(uid, GasAnalyzerUiKey.Key,
            new GasAnalyzerUserMessage(gasMixList.ToArray(),
                component.Target != null ? Name(component.Target.Value) : string.Empty,
                GetNetEntity(component.Target) ?? NetEntity.Invalid,
                deviceFlipped));
        return true;
    }

    /// <summary>
    /// Generates a GasEntry array for a given GasMixture
    /// </summary>
    private GasEntry[] GenerateGasEntryArray(GasMixture? mixture)
    {
        var gases = new List<GasEntry>();

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gas = _atmo.GetGas(i);

            if (mixture?[i] <= UIMinMoles)
                continue;

            if (mixture != null)
            {
                var gasName = Loc.GetString(gas.Name);
                gases.Add(new GasEntry(gasName, mixture[i], gas.Color));
            }
        }

        var gasesOrdered = gases.OrderByDescending(gas => gas.Amount);

        return gasesOrdered.ToArray();
    }
}

/// <summary>
/// Raised when the analyzer is used. An atmospherics device that does not rely on a NodeContainer or
/// wishes to override the default analyzer behaviour of fetching all nodes in the attached NodeContainer
/// should subscribe to this and return the GasMixtures as desired. A device that is flippable should subscribe
/// to this event to report if it is flipped or not. See GasFilterSystem or GasMixerSystem for an example.
/// </summary>
public sealed class GasAnalyzerScanEvent : EntityEventArgs
{
    /// <summary>
    /// The string is for the name (ex "pipe", "inlet", "filter"), GasMixture for the corresponding gas mix. Add all mixes that should be reported when scanned.
    /// </summary>
    public List<(string, GasMixture?)>? GasMixtures;

    /// <summary>
    /// If the device is flipped. Flipped is defined as when the inline input is 90 degrees CW to the side input
    /// </summary>
    public bool DeviceFlipped;
}
