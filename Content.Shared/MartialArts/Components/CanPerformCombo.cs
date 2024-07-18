using Content.Shared.MartialArts;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.MartialArts;

[NetworkedComponent]
[RegisterComponent]
public sealed partial class CanPerformComboComponent : Component
{
    public EntityUid? CurrentTarget;

    public List<ComboAttackType> LastAttacks = new();

    public List<ComboPrototype> AllowedCombos = new();

    [DataField]
    public List<ProtoId<ComboPrototype>> RoundstartCombos = new();

    public TimeSpan ResetTime = TimeSpan.Zero;
}

[Prototype("combo")]
public sealed partial class ComboPrototype : IPrototype
{
    [IdDataField] public string ID { get; private init; } = default!;

    [DataField("attacks", required: true)]
    public List<ComboAttackType> AttackTypes = new();

    //[DataField("weapon")] // Will be done later
    //public string? WeaponProtoId;

    [DataField("event", required: true)]
    public object? ResultEvent;
}
