using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Payloads;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised on a <see cref="DeviceNetworkRouterComponent"/> when it tries to override its normal transmit frequency
/// to send the next <see cref="IRoutableNetworkPayload"/> through the chain to the final destination.
/// </summary>
/// <param name="OverrideTransmit">
/// The frequency to use for the transmission of the next packet.
/// If null, will use <see cref="DeviceNetworkComponent.TransmitFrequency"/> just as usual.
/// </param>
/// <param name="Handled">Whenever some other system had already set the frequency or not.</param>
[ByRefEvent]
public record struct DeviceRouterFrequencyOverrideEvent(uint? OverrideTransmit, bool Handled = false);
