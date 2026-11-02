// Content.Shared/_Stargazer/SpiderLegs/SpiderGripComponent.cs
namespace Content.Shared._Stargazer.SpiderLegs;

/// <summary>
/// Паук держится ногами за станцию: пока в радиусе GripRange от корпуса есть хоть одна клетка
/// станции (пол или стена), сущность не считается невесомой и ходит как при гравитации —
/// даже без генератора и даже снаружи, вдоль обшивки. Дальше от станции хватка теряется
/// и сущность дрейфует как обычно.
/// Работает через IsWeightlessEvent, так же как магнитные ботинки, ничего в апстриме не меняя.
/// </summary>
[RegisterComponent]
public sealed partial class SpiderGripComponent : Component
{
    /// <summary>
    /// Радиус досягаемости ног от центра корпуса, тайлов. Держите близким к MaxReach ног
    /// (чуть меньше — чтобы, пока есть хватка, хотя бы одна нога точно дотягивалась до корпуса).
    /// </summary>
    [DataField]
    public float GripRange = 2.6f;

    /// <summary>Как часто перепроверять хватку, секунд.</summary>
    [DataField]
    public float CheckInterval = 0.1f;

    /// <summary>Есть ли сейчас за что держаться.</summary>
    [ViewVariables]
    public bool Gripping;

    [ViewVariables]
    public float NextCheck;
}
