using Content.Shared.Disposal.Router;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components;

/// <summary>
/// Attached to disposal tubes that can direct entities down different
/// paths depending on what tags they both possess.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DisposalRouterSystem))]
public sealed partial class DisposalRouterComponent : Component
{
    /// <summary>
    /// List of tags that will be compared against the tags possessed
    /// by any disposal holders that pass by. If the tag lists overlap,
    /// the disposal holder will be routed via the first exit, if possible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Tags = new();

    /// <summary>
    /// Sound played when <see cref="Tags"/> is updated by a player.
    /// </summary>
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}

/// <summary>
/// Sends tag data from disposal router UIs to the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class DisposalRouterUiActionMessage : BoundUserInterfaceMessage
{
    public readonly string Tags = string.Empty;

    public DisposalRouterUiActionMessage(string tags, int tagLength)
    {
        Tags = tags.Substring(0, Math.Min(tags.Length, tagLength));
    }
}

/// <summary>
/// Sends tag data to disposal router UIs.
/// </summary>
[Serializable, NetSerializable]
public sealed class DisposalRouterUserInterfaceState : BoundUserInterfaceState
{
    public readonly string Tags;

    public DisposalRouterUserInterfaceState(string tags)
    {
        Tags = tags;
    }
}

/// <summary>
/// Key for opening the disposal router UI.
/// </summary>
[Serializable, NetSerializable]
public enum DisposalRouterUiKey
{
    Key
}
