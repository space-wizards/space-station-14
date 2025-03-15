using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.DeviceLinking.Components;

[RegisterComponent]
public sealed partial class DeadMansSignallerComponent : Component
{
    /// <summary>
    ///     The port that gets signaled when the switch turns on.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "Pressed";
}
