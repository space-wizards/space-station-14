using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Thief;

[Serializable, NetSerializable]
public sealed class ThiefBackpackBoundUserInterfaceState : BoundUserInterfaceState
{
    //переменные всякие
    public readonly int SelectedSets;
    //public readonly int TargetLevel;

    public ThiefBackpackBoundUserInterfaceState(int selectedSets/* тут все переменные что выше*/)
    {
        SelectedSets = selectedSets;
        //инициализация этих переменных
    }

}

[Serializable, NetSerializable]
public enum ThiefBackpackUIKey
{
    Key
};
