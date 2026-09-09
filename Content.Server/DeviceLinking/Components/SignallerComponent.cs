using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Sends out a signal to machine linked objects when used in hand (Z).
/// </summary>
[RegisterComponent]
public sealed partial class SignallerComponent : Component
{
    /// <summary>
    /// The port that gets invoked when used.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> Port = "Pressed";
}
