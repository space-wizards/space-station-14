using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.ActionTypes;

// These are just prototype definitions for actions. Allows actions to be defined once in yaml and re-used elsewhere.
// Note that you still need to create a new instance of each action to properly track the state (cooldown, toggled,
// enabled, etc). The prototypes should not be modified directly.
//
// If ever action states data is separated from the rest of the data, this might not be required
// anymore.

[Prototype("worldTargetAction")]
public readonly record struct WorldTargetActionPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [IncludeDataField] public readonly WorldTargetAction WorldTargetAction = default!;
}

[Prototype("entityTargetAction")]
public readonly record struct EntityTargetActionPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [IncludeDataField] public readonly EntityTargetAction EntityTargetAction = default!;
}

[Prototype("instantAction")]
public readonly record struct InstantActionPrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [IncludeDataField] public readonly InstantAction InstantAction = default!;
}

