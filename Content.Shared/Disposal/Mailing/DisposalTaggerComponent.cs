using Content.Shared.Disposal.Tube;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components;

/// <summary>
/// Disposal holders that pass through this pipe will be marked with the tag
/// specified by <see cref="Tag"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisposalTaggerComponent : DisposalTubeComponent
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
public sealed class DisposalTaggerUserInterfaceState : BoundUserInterfaceState
{
    public readonly string Tag;

    public DisposalTaggerUserInterfaceState(string tag)
    {
        Tag = tag;
    }
}

[Serializable, NetSerializable]
public sealed class DisposalTaggerUiActionMessage : BoundUserInterfaceMessage
{
    public readonly DisposalTaggerUiAction Action;
    public readonly string Tag = "";

    public DisposalTaggerUiActionMessage(DisposalTaggerUiAction action, string tag)
    {
        Action = action;

        if (Action == DisposalTaggerUiAction.Ok)
        {
            Tag = tag.Substring(0, Math.Min(tag.Length, 30));
        }
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
