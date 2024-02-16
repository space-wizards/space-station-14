using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Bql;

[Serializable, NetSerializable]
public sealed class ToolshedVisualizeEuiState : EuiStateBase
{
    public readonly (string name, NetEntity entity)[] Entities;

    public ToolshedVisualizeEuiState((string name, NetEntity entity)[] entities)
    {
        Entities = entities;
    }
}
