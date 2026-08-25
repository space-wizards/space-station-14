using Content.Shared.Disposal.Tagger;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components;

/// <summary>
/// Entities that pass through disposal tubes with this component can be marked with a tag.
/// Entities flushed from a disposal unit with this component can be marked with a tag.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DisposalTaggerSystem))]
public sealed partial class DisposalTaggerComponent : Component
{
    /// <summary>
    /// Tag to apply to passing or flushing entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Tag = string.Empty;

    /// <summary>
    /// Sound played when <see cref="Tag"/> is changed by a player.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    /// <summary>
    /// If false editing the tag is disabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Editable = true;
}

/// <summary>
/// Sends tag data from disposal tagger UIs to the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class DisposalTaggerUiActionMessage : BoundUserInterfaceMessage
{
    public readonly string Tags = string.Empty;

    public DisposalTaggerUiActionMessage(string tags, int tagLength)
    {
        Tags = tags.Substring(0, Math.Min(tags.Length, tagLength));
    }
}

/// <summary>
/// A message to opens the disposal tagger UI sent from a separate UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class DisposalTaggerOpenUiMessage : BoundUserInterfaceMessage;

/// <summary>
/// Key for opening disposal tagger UIs.
/// </summary>
[Serializable, NetSerializable]
public enum DisposalTaggerUiKey
{
    Key
}
