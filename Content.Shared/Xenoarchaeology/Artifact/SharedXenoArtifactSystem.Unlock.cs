using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private EntityQuery<XenoArtifactUnlockingComponent> _unlockingQuery;

    private void InitializeUnlock()
    {
        _unlockingQuery = GetEntityQuery<XenoArtifactUnlockingComponent>();

        SubscribeLocalEvent<XenoArtifactUnlockingComponent, MapInitEvent>(OnUnlockingStarted);
    }

    /// <summary> Finish unlocking phase when the time is up. </summary>
    private void UpdateUnlock(float _)
    {
        var query = EntityQueryEnumerator<XenoArtifactUnlockingComponent, XenoArtifactComponent>();
        while (query.MoveNext(out var uid, out var unlock, out var comp))
        {
            if (_timing.CurTime < unlock.EndTime)
                continue;

            FinishUnlockingState((uid, unlock, comp));
        }
    }

    /// <summary>
    /// Checks if node can be unlocked.
    /// Only those nodes, that have no predecessors, or have all
    /// predecessors unlocked can be unlocked themselves.
    /// Artifact being suppressed also prevents unlocking.
    /// </summary>
    public bool CanUnlockNode(Entity<XenoArtifactNodeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var artifact = GetEntity(ent.Comp.Attached);
        if (!TryComp<XenoArtifactComponent>(artifact, out var artiComp))
            return false;

        if (artiComp.Suppressed)
            return false;

        if (!HasUnlockedPredecessor((artifact.Value, artiComp), ent)
            // unlocked final nodes should not listen for unlocking
            || (!ent.Comp.Locked && GetSuccessorNodes((artifact.Value, artiComp), (ent.Owner, ent.Comp)).Count == 0)
            )
            return false;

        return true;
    }

    /// <summary>
    /// Finishes unlocking phase, removing related component, and sums up what nodes were triggered,
    /// that could be unlocked. Marks such nodes as unlocked, and pushes their node activation event.
    /// </summary>
    public void FinishUnlockingState(Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent)
    {
        string unlockAttemptResultMsg;
        XenoArtifactComponent artifactComponent = ent;
        XenoArtifactUnlockingComponent unlockingComponent = ent;

        SoundSpecifier? soundEffect;
        if (TryGetNodeFromUnlockState(ent, out var node))
        {
            SetNodeUnlocked((ent, artifactComponent), node.Value);
            ActivateNode((ent, ent), (node.Value, node.Value), null, null, Transform(ent).Coordinates, false);
            unlockAttemptResultMsg = "artifact-unlock-state-end-success";

            // as an experiment - unlocking node doesn't activate it, activation is left for player to decide.
            // var activated = ActivateNode((ent, artifactComponent), node.Value, null, null, Transform(ent).Coordinates, false);
            // if (activated)
            soundEffect = unlockingComponent.UnlockActivationSuccessfulSound;
        }
        else
        {
            unlockAttemptResultMsg = "artifact-unlock-state-end-failure";
            soundEffect = unlockingComponent.UnlockActivationFailedSound;
        }

        if (_net.IsServer)
        {
            _popup.PopupEntity(Loc.GetString(unlockAttemptResultMsg), ent);
            _audio.PlayPvs(soundEffect, ent.Owner);
        }

        RemComp(ent, unlockingComponent);
        RaiseUnlockingFinished(ent, node);
        artifactComponent.NextUnlockTime = _timing.CurTime + artifactComponent.UnlockStateRefractory;
    }

    public void CancelUnlockingState(Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent)
    {
        RemComp(ent, ent.Comp1);
        RaiseUnlockingFinished(ent, null);
    }

    /// <summary>
    /// Gets first locked node that can be unlocked (it is locked and all predecessor are unlocked).
    /// </summary>
    public bool TryGetNodeFromUnlockState(
        Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent,
        [NotNullWhen(true)] out Entity<XenoArtifactNodeComponent>? node
    )
    {
        node = null;
        var potentialNodes = new ValueList<Entity<XenoArtifactNodeComponent>>();

        var artifactUnlockingComponent = ent.Comp1;
        foreach (var nodeIndex in GetAllNodeIndices((ent, ent)))
        {
            var artifactComponent = ent.Comp2;
            var curNode = GetNode((ent, artifactComponent), nodeIndex);
            if (!curNode.Comp.Locked || !CanUnlockNode((curNode, curNode)))
                continue;

            var requiredIndices = GetPredecessorNodes((ent, artifactComponent), nodeIndex);
            requiredIndices.Add(nodeIndex);

            if (!ent.Comp1.ArtifexiumApplied)
            {
                // Make sure the two sets are identical
                if (requiredIndices.Count != artifactUnlockingComponent.TriggeredNodeIndexes.Count
                    || !artifactUnlockingComponent.TriggeredNodeIndexes.All(requiredIndices.Contains))
                    continue;

                node = curNode;
                return true; // exit early
            }

            // If we apply artifexium, check that the sets are identical EXCEPT for one extra node.
            // This node is a "wildcard" and we'll make a pool so we can pick one to actually unlock.
            if (!artifactUnlockingComponent.TriggeredNodeIndexes.All(requiredIndices.Contains) ||
                requiredIndices.Count - 1 != artifactUnlockingComponent.TriggeredNodeIndexes.Count)
                continue;

            potentialNodes.Add(curNode);
        }

        if (potentialNodes.Count != 0)
            node = RobustRandom.Pick(potentialNodes);

        return node != null;
    }

    private void OnUnlockingStarted(Entity<XenoArtifactUnlockingComponent> ent, ref MapInitEvent args)
    {
        var unlockingStartedEvent = new ArtifactUnlockingStartedEvent();
        RaiseLocalEvent(ent.Owner, ref unlockingStartedEvent);
    }

    private void RaiseUnlockingFinished(
        Entity<XenoArtifactUnlockingComponent, XenoArtifactComponent> ent,
        Entity<XenoArtifactNodeComponent>? node
    )
    {
        var unlockingFinishedEvent = new ArtifactUnlockingFinishedEvent(node);
        RaiseLocalEvent(ent.Owner, ref unlockingFinishedEvent);
    }

}

/// <summary>
/// Event for starting artifact unlocking stage.
/// </summary>
[ByRefEvent]
public record struct ArtifactUnlockingStartedEvent;

/// <summary>
/// Event for finishing artifact unlocking stage.
/// </summary>
/// <param name="UnlockedNode">Node which were unlocked. Null if stage was finished without new unlocks.</param>
[ByRefEvent]
public record struct ArtifactUnlockingFinishedEvent(EntityUid? UnlockedNode);
