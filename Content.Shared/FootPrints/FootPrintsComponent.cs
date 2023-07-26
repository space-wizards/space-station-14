using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Shared.FootPrints;
[RegisterComponent]
public sealed class FootPrintsComponent : Component
{
    //Visual start
    [ViewVariables(VVAccess.ReadOnly), DataField("leftBarePrint")]
    public string LeftBarePrint = "footprint-left-bare-human";
    [ViewVariables(VVAccess.ReadOnly), DataField("rightBarePrint")]
    public string RightBarePrint { get; } = "footprint-right-bare-human";

    [ViewVariables(VVAccess.ReadOnly), DataField("shoesPrint")]
    public string ShoesPrint = "footprint-shoes";

    [ViewVariables(VVAccess.ReadOnly), DataField("suitPrint")]
    public string SuitPrint = "footprint-suit";

    //Visual end

    [ViewVariables(VVAccess.ReadWrite), DataField("offsetCenter")]
    public Vector2 OffsetCenter = new Vector2(-0.5f, -1f); // Смещение для центра следов

    [ViewVariables(VVAccess.ReadWrite), DataField("offsetPrint")]
    public Vector2 OffsetPrint = new Vector2(0.1f, 0f); // Смещение для самого следа, второй след всегда с зеркальной стороны

    [ViewVariables(VVAccess.ReadOnly), DataField("color")]
    public Color PrintsColor = Color.FromHex("#00000000");// Цвет следов

    [ViewVariables(VVAccess.ReadWrite), DataField("stepSize")]
    public float StepSize = 0.6f; // Дистанция между следами при 1 - один тайл
    public bool RightStep = true; // Переменная для переключения правого и левого следов
    public Vector2 StepPos = Vector2.Zero; //Позиция отсчёта шаг

    [ViewVariables(VVAccess.ReadWrite), DataField("colorQuantity")]
    public float ColorQuantity = 0f; //Количество "грязи" на обуви
    [ViewVariables(VVAccess.ReadWrite), DataField("colorReduceAlpha")]
    public float ColorReduceAlpha = 0.1f;
    [ViewVariables(VVAccess.ReadWrite), DataField("colorQuantityMax")]
    public float ColorQuantityMax = 1.2f;
}
