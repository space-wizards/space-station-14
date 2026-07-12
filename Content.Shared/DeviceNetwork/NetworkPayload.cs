using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// A data class for information passing through a Device Network.
/// </summary>
public interface INetworkPayload
{
    void RaiseEvent(EntityUid target, IDevicePayloadRaiser raiser, ref DeviceNetworkPacketData packet);
}

/// <inheritdoc cref="INetworkPayload"/>
[ImplicitDataDefinitionForInheritors]
public abstract partial class NetworkPayload : INetworkPayload
{
    public abstract void RaiseEvent(EntityUid target, IDevicePayloadRaiser raiser, ref DeviceNetworkPacketData packet);
}

/// <inheritdoc cref="INetworkPayload"/>
/// <typeparam name="T">Type of the payload, has to be the same as the final inherited type.</typeparam>
public abstract partial class NetworkPayloadBase<T> : NetworkPayload where T : NetworkPayloadBase<T>
{
    public override void RaiseEvent(EntityUid target, IDevicePayloadRaiser raiser, ref DeviceNetworkPacketData packet)
    {
        if (this is not T type)
            return;

        raiser.RaisePayloadEvent(target, type, ref packet);
    }
}
