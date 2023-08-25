using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.ActionTypes;

// These are just prototype definitions for actions. Allows actions to be defined once in yaml and re-used elsewhere.
// Note that you still need to create a new instance of each action to properly track the state (cooldown, toggled,
// enabled, etc). The prototypes should not be modified directly.
//
// If ever action states data is separated from the rest of the data, this might not be required
// anymore.

[Prototype("worldTargetAction")]
public sealed partial class WorldTargetActionPrototype : WorldTargetAction, IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    // This is a shitty hack to get around the fact that action-prototypes should not in general be sever-exclusive
    // prototypes, but some actions may need to use server-exclusive events, and there is no way to specify on a
    // per-prototype basis whether the client should ignore it when validating yaml.
    [DataField("serverEvent", serverOnly: true)]
    public WorldTargetActionEvent? ServerEvent
    {
        get => Event;
        set => Event = value;
    }
}

[Prototype("entityTargetAction")]
public sealed partial class EntityTargetActionPrototype : EntityTargetAction, IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("serverEvent", serverOnly: true)]
    public EntityTargetActionEvent? ServerEvent
    {
        get => Event;
        set => Event = value;
    }
}

[Prototype("instantAction")]
public sealed partial class InstantActionPrototype : InstantAction, IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("serverEvent", serverOnly: true)]
    public InstantActionEvent? ServerEvent
    {
        get => Event;
        set => Event = value;
    }
}

