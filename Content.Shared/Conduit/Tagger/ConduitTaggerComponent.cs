using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Conduit.Tagger;

/// <summary>
/// Entities that pass through conduits with this component can be marked with a tag.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ConduitTaggerSystem))]
public sealed partial class ConduitTaggerComponent : Component
{
    /// <summary>
    /// Tag to apply to passing entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Tag = string.Empty;

    /// <summary>
    /// Sound played when <see cref="Tag"/> is changed by a player.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}

/// <summary>
/// Sends tag data from disposal tagger UIs to the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConduitTaggerUiActionMessage : BoundUserInterfaceMessage
{
    public readonly ConduitTaggerUiAction Action;
    public readonly string Tags = string.Empty;

    public ConduitTaggerUiActionMessage(ConduitTaggerUiAction action, string tags, int tagLength)
    {
        Action = action;

        if (Action == ConduitTaggerUiAction.Ok)
        {
            Tags = tags.Substring(0, Math.Min(tags.Length, tagLength));
        }
    }
}

/// <summary>
/// Sends tag data to disposal tagger UIs.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConduitTaggerUserInterfaceState : BoundUserInterfaceState
{
    public readonly string Tags;

    public ConduitTaggerUserInterfaceState(string tags)
    {
        Tags = tags;
    }
}

/// <summary>
/// Type of UI action for the disposal taggers to take.
/// </summary>
[Serializable, NetSerializable]
public enum ConduitTaggerUiAction
{
    Ok
}
