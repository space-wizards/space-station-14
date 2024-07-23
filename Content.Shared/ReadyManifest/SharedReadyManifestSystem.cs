using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ReadyManifest;


[Serializable, NetSerializable]
public sealed class RequestReadyManifestMessage : EntityEventArgs
{
    public NetEntity Id { get; }

    public RequestReadyManifestMessage()
    {
        //Id = id;
    }
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