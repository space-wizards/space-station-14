// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus.Symptoms;
using Content.Shared.DeadSpace.Virus.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Virus.Symptoms;

public sealed class RespiratoryFailureSymptom : VirusSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    public override VirusSymptom Type => VirusSymptom.RespiratoryFailure;
    protected override ProtoId<VirusSymptomPrototype> PrototypeId => "RespiratoryFailureSymptom";
    private const string DamageType = "Asphyxiation";
    private const float DamageAmount = 4.5f;

    public RespiratoryFailureSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow) { }

    public override void OnAdded(EntityUid host, VirusComponent virus) => base.OnAdded(host, virus);
    public override void OnRemoved(EntityUid host, VirusComponent virus) => base.OnRemoved(host, virus);
    public override void OnUpdate(EntityUid host, VirusComponent virus) => base.OnUpdate(host, virus);

    public override void DoEffect(EntityUid host, VirusComponent virus)
    {
        var damageableSystem = _entityManager.System<DamageableSystem>();
        DamageSpecifier dspec = new();
        dspec.DamageDict.Add(DamageType, DamageAmount);
        damageableSystem.TryChangeDamage(host, dspec, true);
    }

    public override IVirusSymptom Clone()
    {
        return new RespiratoryFailureSymptom(EffectTimedWindow.Clone());
    }
}