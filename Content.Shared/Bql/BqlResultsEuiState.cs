using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Bql;

[Serializable, NetSerializable]
public sealed class BqlResultsEuiState : EuiStateBase
{
    public readonly (string name, EntityUid entity)[] Entities;

    public BqlResultsEuiState((string name, EntityUid entity)[] entities)
    {
        Entities = entities;
    }
}
