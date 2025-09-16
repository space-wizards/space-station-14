using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ReadyManifest;

/// <summary>
///     A message to send to the server when requesting a ready manifest.
///     ReadyManifestSystem will open an EUI that will be updated whenever
///     a player changes their ready status.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestReadyManifestMessage : EntityEventArgs
{
    public RequestReadyManifestMessage() { }
}

[Serializable, NetSerializable]
public sealed class ReadyManifestEuiState : EuiStateBase
{
    public Dictionary<ProtoId<JobPrototype>, int> JobCounts { get; }

    public ReadyManifestEuiState(Dictionary<ProtoId<JobPrototype>, int> jobCounts)
    {
        JobCounts = jobCounts;
    }
}
