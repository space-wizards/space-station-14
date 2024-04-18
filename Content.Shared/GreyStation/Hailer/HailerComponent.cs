using Content.Shared.Actions;
using Content.Shared.Dataset;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.GreyStation.Hailer;

/// <summary>
/// Gives this clothing a hailer action to shout a random phrase and play a sound.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedHailerSystem))]
public sealed partial class HailerComponent : Component
{
    /// <summary>
    /// Action to grant when worn that uses <see cref="HailerActionEvent"/>.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Action = string.Empty;

    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// Sound to play when using the action.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Sound to play only when emagged.
    /// Uses the default sound if null.
    /// </summary>
    [DataField]
    public SoundSpecifier? EmaggedSound;
}

/// <summary>
/// Action event to use a hailer
/// </summary>
public sealed partial class HailerActionEvent : InstantActionEvent
{
    /// <summary>
    /// Lines to choose from when out of combat mode and not emagged.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DatasetPrototype> Normal = string.Empty;

    /// <summary>
    /// Lines to choose from when in combat mode and not emagged.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DatasetPrototype> Combat = string.Empty;

    /// <summary>
    /// Lines to choose from when emagged.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DatasetPrototype> Emagged = string.Empty;
}
