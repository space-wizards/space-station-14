using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Piping.EntitySystems;

[UsedImplicitly]
public sealed class AtmosUnsafeUnanchorSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly NodeGroupSystem _group = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";
    private static readonly ProtoId<ConstructionGraphPrototype> MachineGraph = "Machine";

    public override void Initialize()
    {
        SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UserUnanchoredEvent>(OnUserUnanchored);
        SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, BreakageEventArgs>(OnBreak);
        SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, MachineDeconstructedEvent>(OnMachineDeconstructed);
        SubscribeLocalEvent<AtmosUnsafeUnanchorComponent, InteractUsingEvent>(OnInteractUsing, after: [typeof(ConstructionSystem)]);
    }

    private bool IsUnsafe(Entity<AtmosUnsafeUnanchorComponent> ent)
    {
        if (!ent.Comp.Enabled || !TryComp(ent, out NodeContainerComponent? nodes))
            return false;

        if (_atmosphere.GetContainingMixture(ent.Owner, true) is not { } environment)
            return false;

        foreach (var node in nodes.Nodes.Values)
        {
            if (node is not PipeNode pipe)
                continue;

            if (pipe.Air.Pressure - environment.Pressure > 2 * Atmospherics.OneAtmosphere)
            {
                return true;
            }
        }

        return false;
    }

    // Handle unsafe machine deconstruction. Currently applies to gas heaters.
    private void OnInteractUsing(Entity<AtmosUnsafeUnanchorComponent> ent, ref InteractUsingEvent args)
    {
        if (!IsMachineDeconstructInteraction(ent, args) || !IsUnsafe(ent))
            return;

        _popup.PopupEntity(Loc.GetString("comp-atmos-unsafe-deconstruction-warning"), ent,
            args.User, PopupType.MediumCaution);
    }

    // Check if we're about to deconstruct the machine.
    private bool IsMachineDeconstructInteraction(Entity<AtmosUnsafeUnanchorComponent> ent, InteractUsingEvent args)
    {
        if (!args.Handled || !_toolSystem.HasQuality(args.Used, PryingQuality))
            return false;

        if (!TryComp(ent, out ConstructionComponent? construction) || construction.Graph != MachineGraph)
            return false;

        return true;
    }

    private void OnUnanchorAttempt(Entity<AtmosUnsafeUnanchorComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (IsUnsafe(ent))
        {
            args.Delay += 2f;
            _popup.PopupEntity(Loc.GetString("comp-atmos-unsafe-unanchor-warning"), ent,
                args.User, PopupType.MediumCaution);
            return; // Show the warning only once.
        }
    }

    // When unanchoring a pipe, leak the gas that was inside the pipe element.
    // At this point the pipe has been scheduled to be removed from the group, but that won't happen until the next Update() call in NodeGroupSystem,
    // so we have to force an update.
    // This way the gas inside other connected pipes stays unchanged, while the removed pipe is completely emptied.
    private void OnUserUnanchored(Entity<AtmosUnsafeUnanchorComponent> ent, ref UserUnanchoredEvent args)
    {
        if (ent.Comp.Enabled)
        {
            _group.ForceUpdate();
            LeakGas(ent);
        }
    }

    private void OnBreak(Entity<AtmosUnsafeUnanchorComponent> ent, ref BreakageEventArgs args)
    {
        LeakGas(ent, false);
        // Can't use DoActsBehavior["Destruction"] in the same trigger because that would prevent us
        // from leaking. So we make up for this by queueing deletion here.
        QueueDel(ent);
    }

    private void OnMachineDeconstructed(Entity<AtmosUnsafeUnanchorComponent> ent, ref MachineDeconstructedEvent args)
    {
        LeakGas(ent, false);
    }

    /// <summary>
    /// Leak gas from the uid's NodeContainer into the tile atmosphere.
    /// Setting removeFromPipe to false will duplicate the gas inside the pipe intead of moving it.
    /// This is needed to properly handle the gas in the pipe getting deleted with the pipe.
    /// </summary>
    public void LeakGas(Entity<AtmosUnsafeUnanchorComponent> ent, bool removeFromPipe = true)
    {
        if (!TryComp(ent, out NodeContainerComponent? nodes))
            return;

        if (_atmosphere.GetContainingMixture(ent.Owner, true, true) is not { } environment)
            environment = GasMixture.SpaceGas;

        var buffer = new GasMixture();

        foreach (var node in nodes.Nodes.Values)
        {
            if (node is not PipeNode pipe)
                continue;

            if (removeFromPipe)
                _atmosphere.Merge(buffer, pipe.Air.RemoveVolume(pipe.Volume));
            else
            {
                var copy = new GasMixture(pipe.Air); //clone, then remove to keep the original untouched
                _atmosphere.Merge(buffer, copy.RemoveVolume(pipe.Volume));
            }
        }

        _atmosphere.Merge(environment, buffer);
    }
}
