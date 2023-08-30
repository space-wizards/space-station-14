using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Materials;

[RegisterComponent, NetworkedComponent]
public sealed partial class InsertingMaterialStorageComponent : Component
{
    /// <summary>
    /// The time when insertion ends.
    /// </summary>
    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime;

    [ViewVariables]
    public Color? MaterialColor;
}

[Serializable, NetSerializable]
public sealed class InsertingMaterialStorageComponentState : ComponentState
{
    public TimeSpan EndTime;
    public Color? MaterialColor;

    public InsertingMaterialStorageComponentState(TimeSpan endTime, Color? materialColor)
    {
        EndTime = endTime;
        MaterialColor = materialColor;
    }
}
