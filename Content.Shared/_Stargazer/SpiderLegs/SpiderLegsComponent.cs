// Content.Shared/_Stargazer/SpiderLegs/SpiderLegsComponent.cs
// Компонент лежит в Shared, чтобы прототип грузился и на сервере, и на клиенте.
// Сервер его никак не использует: ноги, следы, тряска и звук шагов — чисто клиентские,
// ничего не синхронизируется по сети и не создаёт сущностей.
using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Audio;

namespace Content.Shared._Stargazer.SpiderLegs;

[RegisterComponent]
public sealed partial class SpiderLegsComponent : Component
{
    // ---------- Внешний вид ----------

    /// <summary>Путь к RSI, например "_Stargazer/Objects/Vehicles/spider_legs.rsi".</summary>
    [DataField(required: true)]
    public string Sprite = string.Empty;

    /// <summary>Стейт сегмента. Рисуйте его горизонтально: ось ноги идёт слева направо.</summary>
    [DataField]
    public string SegmentState = "segment";

    /// <summary>Необязательный стейт "сустава", рисуется в каждой точке стыка сегментов.</summary>
    [DataField]
    public string? JointState;

    /// <summary>Необязательный стейт стопы. Рисуйте её "носком" вправо (по оси ноги).</summary>
    [DataField]
    public string? FootState = "foot";

    /// <summary>Тень под поднятой стопой. Чем выше стопа, тем тень больше и бледнее.</summary>
    [DataField]
    public string? ShadowState = "shadow";

    [DataField]
    public float FootSize = 0.5f;

    /// <summary>Количество сегментов в ноге. Кривая пересэмплируется по длине дуги.</summary>
    [DataField]
    public int Segments = 20;

    /// <summary>Толщина у корпуса и у стопы (в тайлах). Нога плавно сужается.</summary>
    [DataField]
    public float RootThickness = 0.32f;

    [DataField]
    public float FootThickness = 0.12f;

    // ---------- Геометрия ----------

    /// <summary>Длина ноги вдоль кривой, в тайлах.</summary>
    [DataField]
    public float Length = 3.5f;

    /// <summary>
    /// Где вдоль хорды стоят контрольные точки кубической Безье (0 = корень, 1 = стопа).
    /// </summary>
    [DataField]
    public float ControlA = 0.15f;

    [DataField]
    public float ControlB = 0.55f;

    /// <summary>
    /// Насколько каждая контрольная точка сдвигается вбок (доля от высоты колена).
    /// ControlAOffset = 0 — нога выходит из корпуса прямо, а колено поднимается дальше:
    /// так соседние ноги не перекрещиваются у основания.
    /// </summary>
    [DataField]
    public float ControlAOffset = 1f;

    [DataField]
    public float ControlBOffset = 1f;

    // ---------- Шаг ----------

    /// <summary>Нога шагает, если расстояние "корень → стопа" больше этого значения. Держите меньше Length.</summary>
    [DataField]
    public float MaxReach = 3.33f;

    /// <summary>Нога шагает, если корпус наехал на стопу ближе этого расстояния.</summary>
    [DataField]
    public float MinReach = 1.4f;

    /// <summary>
    /// Радиус "мёртвой зоны" вокруг идеальной точки стопы. Пока стопа внутри зоны, нога
    /// не шагает, даже если корпус ушёл вперёд — как у настоящего паука.
    /// </summary>
    [DataField]
    public float StepThreshold = 1.2f;

    /// <summary>
    /// Куда ставится стопа при шаге: на эту долю зоны вперёд по ходу движения.
    /// Ширина шага ≈ StepThreshold * (1 + StepOvershoot).
    /// </summary>
    [DataField]
    public float StepOvershoot = 0.7f;

    /// <summary>Длительность одного шага, в секундах.</summary>
    [DataField]
    public float StepTime = 0.26f;

    /// <summary>На сколько тайлов стопа поднимается над полом (вверх по экрану).</summary>
    [DataField]
    public float StepHeight = 0.55f;

    /// <summary>Насколько сильнее выгибается нога, пока стопа в воздухе (добавка к длине дуги).</summary>
    [DataField]
    public float StepLift = 0.5f;

    /// <summary>
    /// Если нога растянута сильнее, чем Length * MaxExtension * этот множитель, она шагает вне очереди,
    /// не дожидаясь другой группы.
    /// </summary>
    [DataField]
    public float OverstretchFactor = 1.0f;

    /// <summary>
    /// Скорость, с которой раскладка ног доворачивается за трансформом (1/сек).
    /// Тело кресла рисуется с noRot, поэтому плавный доворот ног не расходится с корпусом.
    /// </summary>
    [DataField]
    public float TurnSpeed = 10f;

    // ---------- Препятствия ----------

    /// <summary>Не ставить стопы в стены, окна, двери и т.п.</summary>
    [DataField]
    public bool AvoidWalls = true;

    /// <summary>
    /// Какие слои коллизии считаются "стеной". HighImpassable есть у стен, окон (включая тонкие)
    /// и закрытых дверей, но нет у столов, стульев и машин — по ним ходить можно.
    /// Открытая дверь отключает коллизию, поэтому в проём стопа встаёт.
    /// </summary>
    [DataField]
    public CollisionGroup BlockMask = CollisionGroup.HighImpassable;

    /// <summary>Считать космос (клетку без пола) препятствием.</summary>
    [DataField]
    public bool BlockSpace = true;

    /// <summary>Отступ стопы от края стены, в долях клетки.</summary>
    [DataField]
    public float WallMargin = 0.22f;

    // ---------- Втягивание ----------
    // Длина ноги следует за расстоянием до стопы: у стен и в коридорах стопы ставятся ближе,
    // нога укорачивается и остаётся почти прямой, а лишние сегменты уходят в корпус.

    /// <summary>Самая короткая длина, до которой втягивается нога, в тайлах.</summary>
    [DataField]
    public float MinLength = 0.7f;

    /// <summary>
    /// Во сколько раз нога может выдвинуться сверх Length, когда стопа отстаёт от корпуса.
    /// Без этого задние ноги упираются в предел длины и семенят короткими шагами.
    /// Держите MaxReach примерно равным Length * MaxExtension.
    /// </summary>
    [DataField]
    public float MaxExtension = 1.4f;

    /// <summary>
    /// В космосе, когда зацепиться не за что, нога полностью втягивается:
    /// стопа прячется на таком расстоянии от точки крепления, тайлов.
    /// </summary>
    [DataField]
    public float TuckDistance = 0.3f;

    /// <summary>Скорость втягивания, тайлов/с.</summary>
    [DataField]
    public float RetractSpeed = 7f;

    /// <summary>Скорость выдвигания, тайлов/с (медленнее втягивания — как поршень).</summary>
    [DataField]
    public float ExtendSpeed = 4f;

    // ---------- Покой ----------

    /// <summary>Сколько секунд после остановки ноги стоят на месте, прежде чем подтянуться.</summary>
    [DataField]
    public float IdleDelay = 1.5f;

    /// <summary>Во сколько раз сжимается расстановка ног в покое ("втягивание").</summary>
    [DataField]
    public float IdleRetract = 0.85f;

    /// <summary>В покое нога подтягивается, если стоит дальше этого от своей точки покоя.</summary>
    [DataField]
    public float IdleTolerance = 0.15f;

    // ---------- Удар стопы ----------

    /// <summary>Сила толчка камеры у игрока рядом с ударом стопы (0 — без тряски).</summary>
    [DataField]
    public float ShakeStrength = 0.12f;

    /// <summary>На каком расстоянии (в тайлах) тряска затухает до нуля.</summary>
    [DataField]
    public float ShakeRange = 7f;

    [DataField]
    public SoundSpecifier? StepSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_powerloader_step.ogg")
    {
        Params = AudioParams.Default.WithVolume(-6f).WithVariation(0.12f),
    };

    // ---------- Следы ----------

    /// <summary>Стейт следа. null — без следов.</summary>
    [DataField]
    public string? FootprintState = "footprint";

    [DataField]
    public float FootprintSize = 0.7f;

    /// <summary>Сколько секунд живёт след.</summary>
    [DataField]
    public float FootprintLifetime = 8f;

    /// <summary>Сколько последних секунд жизни след плавно тает.</summary>
    [DataField]
    public float FootprintFadeTime = 5f;

    [DataField]
    public Color FootprintColor = new(0.1f, 0.1f, 0.12f, 0.55f);

    [DataField]
    public List<SpiderLegDef> Legs = new();

    // ---------- Рантайм-состояние (только клиент, не сериализуется) ----------

    [ViewVariables]
    public SpiderLegState[] State = Array.Empty<SpiderLegState>();

    public EntityUid StateParent;
    public Vector2 LastPos;
    public Vector2 Velocity;
    public Angle Heading;
    public float IdleTime;
    public bool LegsReady;
}

/// <summary>
/// Описание одной ноги. Координаты локальные: при движении трансформ поворачивается
/// по направлению хода (SharedMoverController), и "вперёд" у сущности — это -Y.
/// </summary>
[DataDefinition]
public sealed partial class SpiderLegDef
{
    /// <summary>Точка крепления к корпусу, в локальных координатах сущности.</summary>
    [DataField]
    public Vector2 Attach;

    /// <summary>Идеальная точка стопы в покое, в локальных координатах сущности.</summary>
    [DataField]
    public Vector2 Rest;

    /// <summary>В какую сторону выгибается колено: 1 или -1.</summary>
    [DataField]
    public int Bend = 1;

    /// <summary>
    /// Группа походки. Ноги одной группы могут шагать одновременно,
    /// группы чередуются. Для четырёх ног ставьте диагонали в одну группу.
    /// </summary>
    [DataField]
    public int Group;
}

/// <summary>На что опирается стопа.</summary>
public enum SpiderFootHold : byte
{
    /// <summary>Стоит на полу.</summary>
    Floor,

    /// <summary>Кресло в космосе, стопа держится за кромку корпуса станции.</summary>
    Hull,

    /// <summary>Зацепиться не за что — нога полностью втянута.</summary>
    Tucked,
}

public sealed class SpiderLegState
{
    /// <summary>На что сейчас опирается стопа.</summary>
    public SpiderFootHold Hold = SpiderFootHold.Floor;

    /// <summary>Для Hull: направление "в корпус" от точки хвата.</summary>
    public Vector2 HullNormal;

    /// <summary>Точка стопы на полу, в координатах родителя (грида или карты).</summary>
    public Vector2 Ground;

    public Vector2 StepFrom;

    /// <summary>Прогресс шага от 0 до 1. Отрицательное значение означает, что нога стоит.</summary>
    public float StepProgress = -1f;

    public bool Stepping => StepProgress >= 0f;

    /// <summary>Высота стопы над полом, 0..1 (доля от StepHeight).</summary>
    public float Height;

    /// <summary>Точки стыков сегментов в координатах родителя (буфер с запасом, см. JointCount).</summary>
    public Vector2[] Joints = Array.Empty<Vector2>();

    /// <summary>Сколько сегментов сейчас выдвинуто (используются Joints[0..JointCount]).</summary>
    public int JointCount;

    /// <summary>Текущая (сглаженная) длина ноги без учёта подъёма при шаге.</summary>
    public float CurrentLength;
}
