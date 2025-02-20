// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
// Official port from the BACKMEN project. Make sure to review the original repository to avoid license violations.

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Backmen.FootPrint;

[RegisterComponent, NetworkedComponent]
public sealed partial class FootPrintsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("path")]
    public ResPath RsiPath = new("/Textures/_Backmen/Effects/footprints.rsi");

    // all of those are set as a layer
    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string LeftBarePrint = "footprint-left-bare-human";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string RightBarePrint = "footprint-right-bare-human";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string ShoesPrint = "footprint-shoes";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string SuitPrint = "footprint-suit";

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public string[] DraggingPrint =
    [
        "dragging-1",
        "dragging-2",
        "dragging-3",
        "dragging-4",
        "dragging-5",
    ];
    // yea, those

    [ViewVariables(VVAccess.ReadOnly), DataField("protoId")]
    public EntProtoId<FootPrintComponent> StepProtoId = "Footstep";

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Vector2 OffsetPrint = new(0.1f, 0f);

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public Color PrintsColor = Color.FromHex("#00000000");

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float StepSize = 0.7f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float DragSize = 0.5f;
    public bool RightStep = true;
    public Vector2 StepPos = Vector2.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float ColorQuantity;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float ColorReduceAlpha = 0.1f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public string? ReagentToTransfer;
}
