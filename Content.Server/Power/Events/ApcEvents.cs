using Content.Server.Power.Components;

namespace Content.Server.Power.Events;

/// <summary>
/// Sent when a <see cref="ExtensionCableProviderComponent"/> connects to a <see cref="ExtensionCableReceiverComponent"/>
/// </summary>
public sealed class ProviderConnectedEvent : EntityEventArgs
{
    /// <summary>
    /// The <see cref="ExtensionCableProviderComponent"/> that connected.
    /// </summary>
    public Entity<ExtensionCableProviderComponent>? Provider;

    public ProviderConnectedEvent(Entity<ExtensionCableProviderComponent>? provider)
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
    public Entity<ExtensionCableProviderComponent>? Provider;

    public ProviderDisconnectedEvent(Entity<ExtensionCableProviderComponent>? provider)
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
    public Entity<ExtensionCableReceiverComponent> Receiver;

    public ReceiverConnectedEvent(Entity<ExtensionCableReceiverComponent> receiver)
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
    public Entity<ExtensionCableReceiverComponent> Receiver;

    public ReceiverDisconnectedEvent(Entity<ExtensionCableReceiverComponent> receiver)
    {
        Receiver = receiver;
    }
}
