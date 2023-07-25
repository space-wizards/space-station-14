using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Bql;

[Serializable, NetSerializable]
public sealed class RtShellVisualizeEuiState : EuiStateBase
{
    public readonly (string name, EntityUid entity)[] Entities;

    public RtShellVisualizeEuiState((string name, EntityUid entity)[] entities)
    {
        Entities = entities;
    }
}
