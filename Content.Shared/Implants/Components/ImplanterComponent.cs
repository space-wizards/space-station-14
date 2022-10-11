using System.Threading;

namespace Content.Shared.Implants.Components;
[RegisterComponent]
public sealed class ImplanterComponent : Component
{
    //TODO: See if you need to add anything else to the implanter
    //Things like inject only, draw, unremoveable, etc.

    /// <summary>
    /// The time it takes to implant someone else
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("implantTime")]
    public float ImplantTime = 5f;

    public CancellationTokenSource? CancelToken;
}
