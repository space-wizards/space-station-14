using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Server._Goobstation.SpaceWhale.SpaceWhaleSegment;

/// <summary>
/// Цепь сегментов-хвоста. Каждый сегмент следует за предыдущим (или за головой
/// для нулевого), движется через физику (SetLinearVelocity), поэтому честно
/// сталкивается со стенами. Подробности — в TailedEntitySystem.
/// </summary>
[RegisterComponent]
public sealed partial class TailedEntityComponent : Component
{
    [DataField] public int Amount = 30;
    [DataField(required: true)] public EntProtoId Prototype;

    /// <summary>
    /// Желаемое расстояние между центрами соседних сегментов (тайлы).
    /// </summary>
    [DataField] public float Spacing = 0.8f;

    /// <summary>
    /// Максимальная скорость, которую система выставляет сегменту.
    /// Должна быть заметно выше скорости головы, чтобы догонять.
    /// </summary>
    [DataField] public float MaxSegmentSpeed = 36f;

    /// <summary>
    /// Когда максимальное растяжение в цепи (max distToPrev / Spacing)
    /// превышает SlowStartFactor — HeadSpeedMultiplier начинает падать.
    /// </summary>
    [DataField] public float SlowStartFactor = 1.3f;

    /// <summary>
    /// На SlowStopFactor голова полностью встаёт.
    /// Между Start и Stop — линейная интерполяция.
    /// </summary>
    [DataField] public float SlowStopFactor = 1.8f;

    /// <summary>
    /// Сегмент считается "толкающимся" вперёд, если расстояние до предыдущего
    /// больше Spacing × PushDistanceFactor. Используется DamageOnCollideSystem,
    /// чтобы сегмент ломал стены только когда реально упирается.
    /// </summary>
    [DataField] public float PushDistanceFactor = 1.2f;

    /// <summary>
    /// Если сегмент далеко (> Spacing × 8) и не может приблизиться дольше
    /// StuckEmergencyDelay секунд — аварийный телепорт к предыдущему.
    /// </summary>
    [DataField] public float StuckEmergencyDelay = 1.5f;

    // ---------------------------------------------------------------------
    // Wave animation — простая боковая волна.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Амплитуда изгиба между соседними звеньями (радианы). Дополняет
    /// головную волну (HeadWiggle), даёт микро-изгиб по телу.
    /// </summary>
    [DataField] public float BendAmplitude = 0.1f;

    /// <summary>
    /// Частота волны изгиба сегментов (рад/с).
    /// </summary>
    [DataField] public float WaveFrequency = 0.9f;

    /// <summary>
    /// Сдвиг фазы между соседними сегментами (рад). Меньше — длиннее волна
    /// вдоль тела, больше — больше "S"-форм одновременно.
    /// </summary>
    [DataField] public float WavePhaseStep = 0.5f;

    /// <summary>
    /// Сглаживание поворота сегментов (1/сек).
    /// </summary>
    [DataField] public float RotationSmooth = 9f;

    /// <summary>
    /// Боковая "вилка" самой головы при движении (тайлы/сек). Голова идёт к
    /// цели не по прямой, а по змеистой синусоиде — основная S рождается из
    /// траектории, а не накапливается по цепи. 0 — выключено.
    /// </summary>
    [DataField] public float HeadWiggleAmplitude = 1.5f;

    /// <summary>
    /// Частота вилки головы (рад/с). 0.6 ≈ цикл ~10 секунд.
    /// </summary>
    [DataField] public float HeadWiggleFrequency = 0.6f;

    // ---------------------------------------------------------------------
    // Runtime state.
    // ---------------------------------------------------------------------

    [ViewVariables] public List<EntityUid> TailSegments = new();
    [ViewVariables] public List<TailSegmentRuntimeState> SegmentStates = new();
    [ViewVariables] public float HeadSpeedMultiplier = 1f;
}

public sealed class TailSegmentRuntimeState
{
    public Vector2 LastPosition;
    public bool HasLastPosition;
    public float StuckTime;
    public bool IsPushing;
}
