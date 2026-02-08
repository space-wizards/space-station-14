using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact;

public abstract partial class SharedXenoArtifactSystem
{
    private void InitializeXAT()
    {
        XATRelayLocalEvent<DamageChangedEvent>();
        XATRelayLocalEvent<InteractUsingEvent>();
        XATRelayLocalEvent<PullStartedMessage>();
        XATRelayLocalEvent<AttackedEvent>();
        XATRelayLocalEvent<XATToolUseDoAfterEvent>();
        XATRelayLocalEvent<InteractHandEvent>();
        XATRelayLocalEvent<ReactionEntityEvent>();
        XATRelayLocalEvent<LandEvent>();

        // special case this one because we need to order the messages
        SubscribeLocalEvent<XenoArtifactComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary> Relays artifact events for artifact nodes. </summary>
    protected void XATRelayLocalEvent<T>() where T : notnull
    {
        SubscribeLocalEvent<XenoArtifactComponent, T>(RelayEventToNodes);
    }

    private void OnExamined(Entity<XenoArtifactComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(XenoArtifactComponent)))
        {
            RelayEventToNodes(ent, ref args);
        }
    }

    protected void RelayEventToNodes<T>(Entity<XenoArtifactComponent> ent, ref T args) where T : notnull
    {
        var ev = new XenoArchNodeRelayedEvent<T>(ent, args);

        var nodes = GetAllNodes(ent);
        foreach (var node in nodes)
        {
            RaiseLocalEvent(node, ref ev);
        }
    }

    /// <summary>
    /// Attempts to shift artifact into unlocking state, in which it is going to listen to interactions, that could trigger nodes.
    /// </summary>
    public void TriggerXenoArtifact(Entity<XenoArtifactComponent> ent, Entity<XenoArtifactNodeComponent>? node, bool force = false)
    {
        // limits spontaneous chain activations, also prevents spamming every triggering tool to activate nodes
        // without real knowledge about triggers
        if (!force && _timing.CurTime < ent.Comp.NextUnlockTime)
            return;

        if (!_unlockingQuery.TryGetComponent(ent, out var unlockingComp))
        {
            unlockingComp = EnsureComp<XenoArtifactUnlockingComponent>(ent);
            unlockingComp.EndTime = _timing.CurTime + ent.Comp.UnlockStateDuration;
            Log.Debug($"{ToPrettyString(ent)} entered unlocking state");

            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("artifact-unlock-state-begin"), ent);
            Dirty(ent);
            if (node != null && unlockingComp.TriggeredNodeIndexes.Add(GetIndex(ent, node.Value)))
            {
                Dirty(ent, unlockingComp);
            }
        }
        else if (node != null)
        {
            var index = GetIndex(ent, node.Value);

            // We need to add time, UNLESS the unlocking process is in a failed state after adding the new trigger.
            // An unlockable node will fail to unlock if there is a trigger other than its required triggers.
            // A failing unlocking state is one where there exists no unlockable nodes that have not failed.

            if (unlockingComp.TriggeredNodeIndexes.Add(index))
            {
                var allnodes = GetAllNodes((ent, ent));
                foreach (var nodeEnt in allnodes)
                {
                    if (!nodeEnt.Comp.Locked)
                        continue;
                    var directPredecessorNodes = GetDirectPredecessorNodes((ent, ent), nodeEnt);
                    if (directPredecessorNodes.Count == 0 || directPredecessorNodes.All(x => !x.Comp.Locked))
                    {
                        // This is an unlockable node, check if is failed
                        var predecessorNodeIndices = GetPredecessorNodes((ent, ent), GetIndex(ent, nodeEnt.Owner));
                        if (unlockingComp.TriggeredNodeIndexes.All(x => predecessorNodeIndices.Contains(x)))
                        {
                            unlockingComp.EndTime += ent.Comp.UnlockStateIncrementPerNode; // We have found an unlockable node that is still possible to unlock - it contains all triggers in its predecessors
                            break;
                        }
                    }
                }
            }
            Dirty(ent, unlockingComp);
        }
    }

    public void SetArtifexiumApplied(Entity<XenoArtifactUnlockingComponent> ent, bool val)
    {
        ent.Comp.ArtifexiumApplied = val;
        Dirty(ent);
    }
}

/// <summary>
/// Event wrapper for XenoArch Trigger events.
/// </summary>
[ByRefEvent]
public record struct XenoArchNodeRelayedEvent<TEvent>(Entity<XenoArtifactComponent> Artifact, TEvent Args)
{
    /// <summary>
    /// Original event.
    /// </summary>
    public TEvent Args = Args;

    /// <summary>
    /// Artifact entity, that received original event.
    /// </summary>
    public Entity<XenoArtifactComponent> Artifact = Artifact;
}
