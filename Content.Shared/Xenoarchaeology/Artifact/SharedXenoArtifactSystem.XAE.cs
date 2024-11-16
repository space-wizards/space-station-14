using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Map;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    private void InitializeXAE()
    {
        SubscribeLocalEvent<XenoArtifactComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<XenoArtifactComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<XenoArtifactComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnUseInHand(Entity<XenoArtifactComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryActivateXenoArtifact(ent, args.User, args.User, Transform(args.User).Coordinates);
    }

    private void OnAfterInteract(Entity<XenoArtifactComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        args.Handled = TryActivateXenoArtifact(ent, args.User, args.Target, args.ClickLocation);
    }

    private void OnActivateInWorld(Entity<XenoArtifactComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = TryActivateXenoArtifact(ent, args.User, args.Target, Transform(args.Target).Coordinates);
    }

    public bool TryActivateXenoArtifact(
        Entity<XenoArtifactComponent> artifact,
        EntityUid? user,
        EntityUid? target,
        EntityCoordinates coordinates)
    {
        if (artifact.Comp.Suppressed)
            return false;

        if (TryComp<UseDelayComponent>(artifact, out var delay) && !_useDelay.TryResetDelay((artifact, delay), true))
            return false;

        var success = false;
        foreach (var node in GetActiveNodes(artifact))
        {
            success |= ActivateNode(artifact, node, user, target, coordinates);
        }

        if (!success)
        {
            _popup.PopupClient(Loc.GetString("artifact-activation-fail"), artifact, user);
        }

        return true;
    }

    public bool ActivateNode(
        Entity<XenoArtifactComponent> artifact,
        Entity<XenoArtifactNodeComponent> node,
        EntityUid? user,
        EntityUid? target,
        EntityCoordinates coordinates,
        bool consumeDurability = true
    )
    {
        if (node.Comp.Degraded)
            return false;

        _adminLogger.Add(
            LogType.ArtifactNode,
            LogImpact.Low,
            $"{ToPrettyString(artifact.Owner)} node {ToPrettyString(node)} got activated at {coordinates}"
        );
        if (consumeDurability)
        {
            AdjustNodeDurability((node, node.Comp), -1);
        }

        var ev = new XenoArtifactNodeActivatedEvent(artifact, node, user, target, coordinates);
        RaiseLocalEvent(node, ref ev);
        return true;
    }
}

[ByRefEvent]
public readonly record struct XenoArtifactNodeActivatedEvent(
    Entity<XenoArtifactComponent> Artifact,
    Entity<XenoArtifactNodeComponent> Node,
    EntityUid? User,
    EntityUid? Target,
    EntityCoordinates Coordinates
);
