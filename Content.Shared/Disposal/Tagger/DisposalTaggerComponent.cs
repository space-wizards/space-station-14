using Content.Shared.Disposal.Tagger;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components;

/// <summary>
/// Disposal holders that pass through this pipe will be marked with the specified tag.
/// specified by <see cref="Tag"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DisposalTaggerSystem))]
public sealed partial class DisposalTaggerComponent : Component
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

[Serializable, NetSerializable]
public sealed class DisposalTaggerUiActionMessage : BoundUserInterfaceMessage
{
    public readonly DisposalTaggerUiAction Action;
    public readonly string Tags = string.Empty;

    public DisposalTaggerUiActionMessage(DisposalTaggerUiAction action, string tags, int tagLength)
    {
        Action = action;

        if (Action == DisposalTaggerUiAction.Ok)
        {
            Tags = tags.Substring(0, Math.Min(tags.Length, tagLength));
        }
    }
}

[Serializable, NetSerializable]
public sealed class DisposalTaggerUserInterfaceState : BoundUserInterfaceState
{
    public readonly string Tags;

    public DisposalTaggerUserInterfaceState(string tags)
    {
        Tags = tags;
    }
}

[Serializable, NetSerializable]
public enum DisposalTaggerUiAction
{
    Ok
}

[Serializable, NetSerializable]
public enum DisposalTaggerUiKey
{
    Key
}
