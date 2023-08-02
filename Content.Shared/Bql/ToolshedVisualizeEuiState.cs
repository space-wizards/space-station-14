using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Bql;

[Serializable, NetSerializable]
public sealed class ToolshedVisualizeEuiState : EuiStateBase
{
    public readonly (string name, EntityUid entity)[] Entities;

    public ToolshedVisualizeEuiState((string name, EntityUid entity)[] entities)
    {
        Entities = entities;
    }
}
