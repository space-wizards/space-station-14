using Content.Shared.Gateway;

namespace Content.Server.Gateway;

/// <summary>
/// Raised when trying to retrieve gateway destinations for a particular entity.
/// This is raised after already retrieving GatewayDestinationComponents.
/// </summary>
[ByRefEvent]
public record struct GetGatewayDestinationsEvent
{
    public EntityUid GatewayEntity;

    public List<GatewayDestinationData> Data;
}
