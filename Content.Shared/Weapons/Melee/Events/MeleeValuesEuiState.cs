using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class MeleeValuesEuiState : EuiStateBase
{
    public List<string> Headers = new();
    public List<string[]> Values = new();
}
