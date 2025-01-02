using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
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
    public void TriggerXenoArtifact(Entity<XenoArtifactComponent> ent, Entity<XenoArtifactNodeComponent> node)
    {
        // limits spontaneous chain activations, also prevents spamming every triggering tool to activate nodes
        // without real knowledge about triggers
        if (_timing.CurTime < ent.Comp.NextUnlockTime)
            return;

        var index = GetIndex(ent, node);

        if (!_unlockingQuery.TryGetComponent(ent, out var unlockingComp))
        {
            unlockingComp = EnsureComp<XenoArtifactUnlockingComponent>(ent);
            unlockingComp.EndTime = _timing.CurTime + ent.Comp.UnlockStateDuration;
            Log.Debug($"{ToPrettyString(ent)} entered unlocking state");

            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("artifact-unlock-state-begin"), ent);
        }
        else
        {
            var predecessorNodeIndices = GetPredecessorNodes((ent, ent), index);
            var successorNodeIndices = GetSuccessorNodes((ent, ent), index);
            if(unlockingComp.TriggeredNodeIndexes.Count == 0
               || unlockingComp.TriggeredNodeIndexes.All(
                   x => predecessorNodeIndices.Contains(x) || successorNodeIndices.Contains(x)
                )
               )
                // we add time on each new trigger, if it is not going to fail us
                unlockingComp.EndTime += ent.Comp.UnlockStateIncrementPerNode;
        }

        if (unlockingComp.TriggeredNodeIndexes.Add(index))
        {
            Dirty(ent, unlockingComp);
        }
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
