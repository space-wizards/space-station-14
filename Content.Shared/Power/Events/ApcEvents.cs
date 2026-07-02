using Content.Shared.Power.Components;

namespace Content.Shared.Power.Events;

/// <summary>
/// Sent when a <see cref="ExtensionCableProviderComponent"/> connects to a <see cref="ExtensionCableReceiverComponent"/>
/// </summary>
public sealed class ProviderConnectedEvent : EntityEventArgs
{
    /// <summary>
    /// The <see cref="ExtensionCableProviderComponent"/> that connected.
    /// </summary>
    public EntityUid? Provider;

    public ProviderConnectedEvent(EntityUid? provider)
    {
        Provider = provider;
    }
}

/// <summary>
/// Sent when a <see cref="ExtensionCableProviderComponent"/> disconnects from a <see cref="ExtensionCableReceiverComponent"/>
/// </summary>
public sealed class ProviderDisconnectedEvent : EntityEventArgs
{
    /// <summary>
    /// The <see cref="ExtensionCableProviderComponent"/> that disconnected.
    /// </summary>
    public EntityUid? Provider;

    public ProviderDisconnectedEvent(EntityUid? provider)
    {
        Provider = provider;
    }
}

/// <summary>
/// Sent when a <see cref="ExtensionCableReceiverComponent"/> connects to a <see cref="ExtensionCableProviderComponent"/>
/// </summary>
public sealed class ReceiverConnectedEvent : EntityEventArgs
{
    /// <summary>
    /// The <see cref="ExtensionCableReceiverComponent"/> that connected.
    /// </summary>
    public EntityUid Receiver;

    public ReceiverConnectedEvent(EntityUid receiver)
    {
        Receiver = receiver;
    }
}

/// <summary>
/// Sent when a <see cref="ExtensionCableReceiverComponent"/> disconnects from a <see cref="ExtensionCableProviderComponent"/>
/// </summary>
public sealed class ReceiverDisconnectedEvent : EntityEventArgs
{
    /// <summary>
    /// The <see cref="ExtensionCableReceiverComponent"/> that disconnected.
    /// </summary>
    public EntityUid Receiver;

    public ReceiverDisconnectedEvent(EntityUid receiver)
    {
        Receiver = receiver;
    }
}
