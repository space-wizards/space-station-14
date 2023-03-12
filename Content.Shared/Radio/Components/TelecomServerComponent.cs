namespace Content.Shared.Radio.Components;

/// <summary>
/// Entities with component are needed to to transmit messages using headsets. 
/// They also need to be powered by <see cref="ApcPowerReceiverComponent"/>
/// have <see cref="EncryptionKeyHolderComponent"/> and filled with encryption keys
/// of channels in order for them to work on the same map as server.
/// </summary>
[RegisterComponent]
public sealed class TelecomServerComponent : Component
{
}
