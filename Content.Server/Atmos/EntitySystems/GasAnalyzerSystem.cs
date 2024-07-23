using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using static Content.Shared.Atmos.Components.GasAnalyzerComponent;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasAnalyzerSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly AtmosphereSystem _atmo = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        /// <summary>
        /// Minimum moles of a gas to be sent to the client.
        /// </summary>
        private const float UIMinMoles = 0.01f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<GasAnalyzerComponent, GasAnalyzerDisableMessage>(OnDisabledMessage);
            SubscribeLocalEvent<GasAnalyzerComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<GasAnalyzerComponent, UseInHandEvent>(OnUseInHand);
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
        /// Activates the analyzer when used in the world, scanning either the target entity or the tile clicked
        /// </summary>
        private void OnAfterInteract(EntityUid uid, GasAnalyzerComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
            {
                _popup.PopupEntity(Loc.GetString("gas-analyzer-component-player-cannot-reach-message"), args.User, args.User);
                return;
            }
            ActivateAnalyzer(uid, component, args.User, args.Target);
            args.Handled = true;
        }

        /// <summary>
        /// Activates the analyzer with no target, so it only scans the tile the user was on when activated
        /// </summary>
        private void OnUseInHand(EntityUid uid, GasAnalyzerComponent component, UseInHandEvent args)
        {
            ActivateAnalyzer(uid, component, args.User);
            args.Handled = true;
        }

        /// <summary>
        /// Handles analyzer activation logic
        /// </summary>
        private void ActivateAnalyzer(EntityUid uid, GasAnalyzerComponent component, EntityUid user, EntityUid? target = null)
        {
            if (!TryOpenUserInterface(uid, user, component))
                return;

            component.Target = target;
            component.User = user;
            if (target != null)
                component.LastPosition = Transform(target.Value).Coordinates;
            else
                component.LastPosition = null;
            component.Enabled = true;
            Dirty(uid, component);
            UpdateAppearance(uid, component);
            EnsureComp<ActiveGasAnalyzerComponent>(uid);
            UpdateAnalyzer(uid, component);
        }

        /// <summary>
        /// Close the UI, turn the analyzer off, and don't update when it's dropped
        /// </summary>
        private void OnDropped(EntityUid uid, GasAnalyzerComponent component, DroppedEvent args)
        {
            if (args.User is var userId && component.Enabled)
                _popup.PopupEntity(Loc.GetString("gas-analyzer-shutoff"), userId, userId);
            DisableAnalyzer(uid, component, args.User);
        }

        /// <summary>
        /// Closes the UI, sets the icon to off, and removes it from the update list
        /// </summary>
        private void DisableAnalyzer(EntityUid uid, GasAnalyzerComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _userInterface.CloseUi(uid, GasAnalyzerUiKey.Key, user);

            component.Enabled = false;
            Dirty(uid, component);
            UpdateAppearance(uid, component);
            RemCompDeferred<ActiveGasAnalyzerComponent>(uid);
        }

        /// <summary>
        /// Disables the analyzer when the user closes the UI
        /// </summary>
        private void OnDisabledMessage(EntityUid uid, GasAnalyzerComponent component, GasAnalyzerDisableMessage message)
        {
            DisableAnalyzer(uid, component);
        }

        private bool TryOpenUserInterface(EntityUid uid, EntityUid user, GasAnalyzerComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return false;

            return _userInterface.TryOpenUi(uid, GasAnalyzerUiKey.Key, user);
        }

        /// <summary>
        /// Fetches fresh data for the analyzer. Should only be called by Update or when the user requests an update via refresh button
        /// </summary>
        private bool UpdateAnalyzer(EntityUid uid, GasAnalyzerComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!TryComp(component.User, out TransformComponent? xform))
            {
                DisableAnalyzer(uid, component);
                return false;
            }

            // check if the user has walked away from what they scanned
            var userPos = xform.Coordinates;
            if (component.LastPosition.HasValue)
            {
                // Check if position is out of range => don't update and disable
                if (!_transform.InRange(component.LastPosition.Value, userPos, SharedInteractionSystem.InteractionRange))
                {
                    if (component.User is { } userId && component.Enabled)
                        _popup.PopupEntity(Loc.GetString("gas-analyzer-shutoff"), userId, userId);
                    DisableAnalyzer(uid, component, component.User);
                    return false;
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
                    DisableAnalyzer(uid, component, component.User);
                    return false;
                }

                // gas analyzed was used on an entity, try to request gas data via event for override
                var ev = new GasAnalyzerScanEvent();
                RaiseLocalEvent(component.Target.Value, ev);

                if (ev.GasMixtures != null)
                {
                    foreach (var mixes in ev.GasMixtures)
                    {
                        if (mixes.Item2 != null)
                            gasMixList.Add(new GasMixEntry(mixes.Item1, mixes.Item2.Volume, mixes.Item2.Pressure, mixes.Item2.Temperature, GenerateGasEntryArray(mixes.Item2)));
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
                            }
                        }
                    }
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
        /// Sets the appearance based on the analyzers Enabled state
        /// </summary>
        private void UpdateAppearance(EntityUid uid, GasAnalyzerComponent analyzer)
        {
            _appearance.SetData(uid, GasAnalyzerVisuals.Enabled, analyzer.Enabled);
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
