// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus.Symptoms;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Virus.Prototypes;

[Prototype]
public sealed partial class VirusSymptomPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public string Description { get; private set; } = default!;

    /// <summary>
    ///     Количество прибавляемой заразности симптому в процентах.
    /// </summary>
    [DataField]
    public float AddInfectivity { get; private set; } = 0.02f;

    /// <summary>
    ///     Цена мутации.
    /// </summary>
    [DataField]
    public int Price = 100;

    /// <summary>
    ///     Тип симптома. Должен быть уникальным.
    /// </summary>
    [DataField(required: true)]
    public VirusSymptom SymptomType;

    /// <summary>
    ///     Индикатор, требуется для управления сиптомами в случайных вирусах событий игры.
    /// </summary>
    [DataField("danger", required: true)]
    public DangerIndicatorSymptom DangerIndicator;

    /// <summary>
    ///     Минимальный интервал срабатывания симптома
    /// </summary>
    [DataField]
    public float MinInterval = 15f;

    /// <summary>
    ///     Максимальный интервал срабатывания симптома
    /// </summary>
    [DataField]
    public float MaxInterval = 60f;

    [DataField]
    public bool TaipanOnly = false;

    [DataField]
    public ProtoId<VirusSymptomPrototype>? RequiredSymptom = null;

    [DataField]
    public List<ProtoId<VirusSymptomPrototype>> BlockedBySymptoms = new();
}

public enum DangerIndicatorSymptom
{
    Low = 0,
    Medium,
    High,
    Cataclysm
}

