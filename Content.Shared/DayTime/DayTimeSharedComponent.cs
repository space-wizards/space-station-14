using Robust.Shared.Map.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.DayTime;
[RegisterComponent, NetworkedComponent]
public sealed partial class DayTimeComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), DataField("colorFrom")]
    public Color ColorFrom;
    [ViewVariables(VVAccess.ReadOnly), DataField("colorTo")]
    public Color ColorTo;

    [ViewVariables(VVAccess.ReadOnly), DataField("colorCurrent")]
    public Color ColorCurrent;

    [ViewVariables(VVAccess.ReadOnly), DataField("colorStage")]
    public Color[]? ColorStage = new Color[12]
    {
        Color.FromHex("#FF9430FF"), //восход
        Color.FromHex("#E3D554FF"), //утро
        Color.FromHex("#FCF39AFF"), //полдень
        Color.FromHex("#FCF39AFF"), //полдень
        Color.FromHex("#E8721EFF"), //закат
        Color.FromHex("#6C858CFF"), //сумерки вечерние
        Color.FromHex("#303847FF"), //начало ночи
        Color.FromHex("#060608FF"), //темно
        Color.FromHex("#020203FF"), //пиздец темно
        Color.FromHex("#020203FF"), //пиздец темно
        Color.FromHex("#382E2AFF"), //утренние сумерки начало
        Color.FromHex("#9E664DFF") //утренние сумерки конец
    };

    [ViewVariables(VVAccess.ReadOnly), DataField("timeStage")]
    public float[]? TimeStage = new float[12]
    {
        30f,
        30f,
        30f,
        150f,
        60f,
        30f,
        60f,
        30f,
        30f,
        180f,
        30f,
        60f,

        //720 секунд
        // 12 минут цикл
    };

    [ViewVariables(VVAccess.ReadWrite), DataField("stepsPerSeconds")] // Количество шагов в секунду
    public int StepsPerSecond = 2;

    [ViewVariables(VVAccess.ReadOnly), DataField("currentStage")] // Текущий этап цвета
    public int CurrentStage = 0;

    [ViewVariables(VVAccess.ReadOnly), DataField("stageTime")] // Время
    public float StageTimer;
    [ViewVariables(VVAccess.ReadOnly), DataField("colorTimer")] // Таймер
    public float ColorTimer;
}

