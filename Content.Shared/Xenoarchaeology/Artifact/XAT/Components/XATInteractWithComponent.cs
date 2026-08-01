using Content.Shared.Destructible.Thresholds;
using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact trigger that activates when a user interacts with the artifact using an entity.
/// EG: A user clicks on the artifact whilst holding a carrot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATInteractWithSystem)), AutoGenerateComponentState]
public sealed partial class XATInteractWithComponent : Component
{
    /// <summary>
    /// Whitelist of allowed interacting entities.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    /// <summary>
    /// Whether to destroy the interacting entity afterwards.
    /// EG: feed the artifact a pizza slice, it eats it.
    /// </summary>
    [DataField]
    public bool DestroyAfter = false;

    /// <summary>
    /// Additional Sound played on successful trigger.
    /// </summary>
    [DataField]
    public SoundSpecifier? SuccessTriggerSound;

    /// <summary>
    /// Sound played on beginning interaction.
    /// Keep this or success trigger sound silent if interaction time is zero.
    /// </summary>
    [DataField]
    public SoundSpecifier? StartTriggerSound;

    /// <summary>
    /// DoAfter time of interaction.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan InteractionTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Number of interactions required to trigger.
    /// Interacting with a stack counts a number of interactions equal to the stack count.
    /// </summary>
    [DataField]
    public MinMax InteractionCount = new(1, 1);

    /// <summary>
    /// Number of interactions required to trigger, set after initiation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? MaxCount;

    /// <summary>
    /// Number of interactions to go.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Count;
}

/// <summary>
/// DoAfterEvent for interacting with an artifact using an item.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class XATInteractWithDoAfterEvent : DoAfterEvent
{
    public NetEntity Node;

    public XATInteractWithDoAfterEvent(NetEntity node)
    {
        Node = node;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
