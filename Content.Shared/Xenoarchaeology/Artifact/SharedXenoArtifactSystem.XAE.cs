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

    /// <summary>
    /// Attempts to activate artifact nodes. 'active' are nodes that are marked as 'unlocked' and have no other successors, marked as 'unlocked'.
    /// </summary>
    /// <param name="artifact">Artifact entity, for which attempt to activate was made.</param>
    /// <param name="user">Character that attempted to activate artifact.</param>
    /// <param name="target">Target, on which artifact activation attempt was used (for hand-held artifact - it can be 'clicked' over someone).</param>
    /// <param name="coordinates">Coordinates of <paramref name="target"/> entity.</param>
    /// <param name="consumeDurability">Whether this activation will deplete durability on the activated nodes.</param>
    /// <returns>True, if activation was successful, false otherwise.</returns>
    public bool TryActivateXenoArtifact(
        Entity<XenoArtifactComponent> artifact,
        EntityUid? user,
        EntityUid? target,
        EntityCoordinates coordinates,
        bool consumeDurability = true
    )
    {
        XenoArtifactComponent xenoArtifactComponent = artifact;
        if (xenoArtifactComponent.Suppressed)
            return false;

        if (TryComp<UseDelayComponent>(artifact, out var delay) && !_useDelay.TryResetDelay((artifact, delay), true))
            return false;

        var success = false;
        foreach (var node in GetActiveNodes(artifact))
        {
            success |= ActivateNode(artifact, node, user, target, coordinates, consumeDurability: consumeDurability);
        }

        if (!success)
        {
            _popup.PopupClient(Loc.GetString("artifact-activation-fail"), artifact, user);
            return false;
        }

        // we raised event for each node activation,
        // now we raise event for artifact itself. For animations and stuff.
        var ev = new XenoArtifactActivatedEvent(
            artifact,
            user,
            target,
            coordinates
        );
        RaiseLocalEvent(artifact, ref ev);

        if (user.HasValue)
            _audio.PlayPredicted(xenoArtifactComponent.ForceActivationSoundSpecifier, artifact, user);
        else
            _audio.PlayPvs(xenoArtifactComponent.ForceActivationSoundSpecifier, artifact);

        return true;
    }

    /// <summary>
    /// Pushes node activation event and updates durability for activated node.
    /// </summary>
    /// <param name="artifact">Artifact entity, for which attempt to activate was made.</param>
    /// <param name="node">Node entity, effect of which should be activated.</param>
    /// <param name="user">Character that attempted to activate artifact.</param>
    /// <param name="target">Target, on which artifact activation attempt was used (for hand-held artifact - it can be 'clicked' over someone).</param>
    /// <param name="coordinates">Coordinates of <paramref name="target"/> entity.</param>
    /// <param name="consumeDurability">Marker, if node durability should be adjusted as a result of activation.</param>
    /// <returns>True, if activation was successful, false otherwise.</returns>
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

/// <summary>
/// Event of node activation. Should lead to node effect being activated.
/// </summary>
/// <param name="Artifact">Artifact entity, for which attempt to activate was made.</param>
/// <param name="Node">Node entity, effect of which should be activated.</param>
/// <param name="User">Character that attempted to activate artifact.</param>
/// <param name="Target">Target, on which artifact activation attempt was used (for hand-held artifact - it can be 'clicked' over someone).</param>
/// <param name="Coordinates">Coordinates of <paramref name="Target"/> entity.</param>
[ByRefEvent]
public readonly record struct XenoArtifactNodeActivatedEvent(
    Entity<XenoArtifactComponent> Artifact,
    Entity<XenoArtifactNodeComponent> Node,
    EntityUid? User,
    EntityUid? Target,
    EntityCoordinates Coordinates
);

[ByRefEvent]
public readonly record struct XenoArtifactActivatedEvent(
    Entity<XenoArtifactComponent> Artifact,
    EntityUid? User,
    EntityUid? Target,
    EntityCoordinates Coordinates
);
