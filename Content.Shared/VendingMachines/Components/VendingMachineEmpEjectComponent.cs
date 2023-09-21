using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VendingMachineEmpEjectComponent : Component
{
    /// <summary>
    ///     While disabled by EMP it randomly ejects items
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextEmpEject = TimeSpan.Zero;
}
