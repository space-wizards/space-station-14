using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.ActionTypes;

// These are just prototype definitions for actions. Allows actions to be defined once in yaml and re-used elsewhere.
// Note that you still need to create a new instance of each action to properly track the state (cooldown, toggled,
// enabled, etc). The prototypes should not be modified directly.

[Prototype("worldTargetAction")]
public sealed class WorldTargetActionPrototype : WorldTargetAction, IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;
}

[Prototype("entityTargetAction")]
public sealed class EntityTargetActionPrototype : EntityTargetAction, IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;
}

[Prototype("instantAction")]
public sealed class InstantActionPrototype : InstantAction, IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;
}

