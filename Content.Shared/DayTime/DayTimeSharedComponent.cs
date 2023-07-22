using Robust.Shared.Map.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.DayTime;
[RegisterComponent, NetworkedComponent]
public sealed class DayTimeComponent : Component
{
    [DataField("colorFrom")] //От какого цвета
    public Color ColorFrom;
    [DataField("colorTo")] // К какому цвету
    public Color ColorTo;

    [DataField("colorCurrent")] // Текущий цвет
    public Color ColorCurrent;

    [DataField("colorStage")] // Массив цветов
    public Color[]? ColorStage = new Color[5]
    {
        Color.FromHex("#000000FF"),
        Color.FromHex("#FFAA00FF"),
        Color.FromHex("#000000FF"),
        Color.FromHex("#FF00FFFF"),
        Color.FromHex("#000000FF")
    };

    [DataField("timeStage")] // Массив периодов между цветами
    public float[]? TimeStage = new float[5] { 10f, 10f, 1f, 10f, 5f };

    [DataField("stepsPerSeconds")] // Количество шагов в секунду
    public int StepsPerSecond = 10;

    [DataField("currentStage")] // Текущий этап цвета
    public int CurrentStage = 0;

    [ViewVariables(VVAccess.ReadWrite), DataField("stageTime")] // Время
    public float StageTimer;
    [ViewVariables(VVAccess.ReadOnly), DataField("colorTimer")] // Таймер
    public float ColorTimer;

    public MapLightComponent? MapLightComponent; // Компонент MapLight
}

