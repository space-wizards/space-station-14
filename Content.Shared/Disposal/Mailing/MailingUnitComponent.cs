using Content.Shared.Disposal.Mailing;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components;

[Access(typeof(SharedMailingUnitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MailingUnitComponent : Component
{
    /// <summary>
    /// List of targets the mailing unit can send to.
    /// Each target is just a disposal routing tag
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> TargetList = new();

    /// <summary>
    /// The target that gets attached to the disposal holders tag list on flush
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Target;

    /// <summary>
    /// The tag for this mailing unit
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Tag;
}

/// <summary>
/// Sent before the disposal unit flushes it's contents.
/// Allows adding tags for sorting and preventing the disposal unit from flushing.
/// </summary>
public sealed class BeforeDisposalFlushEvent : CancellableEntityEventArgs
{
    public readonly List<string> Tags = new();
}

/// <summary>
/// Message data sent from client to server when a disposal unit ui button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class TargetSelectedMessage : BoundUserInterfaceMessage
{
    public readonly string? Target;

    public TargetSelectedMessage(string? target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public enum MailingUnitUiKey : byte
{
    Key
}
