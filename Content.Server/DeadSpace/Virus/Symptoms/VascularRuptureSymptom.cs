// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus.Symptoms;
using Content.Shared.DeadSpace.Virus.Components;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Virus.Symptoms;

public sealed class VascularRuptureSymptom : VirusSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    public override VirusSymptom Type => VirusSymptom.VascularRupture;
    protected override ProtoId<VirusSymptomPrototype> PrototypeId => "VascularRuptureSymptom";
    private const float BleedIncrease = 1.5f;

    public VascularRuptureSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow) { }

    public override void OnAdded(EntityUid host, VirusComponent virus)
    {
        base.OnAdded(host, virus);
        ApplyBleed(host);
    }

    public override void OnRemoved(EntityUid host, VirusComponent virus) => base.OnRemoved(host, virus);
    public override void OnUpdate(EntityUid host, VirusComponent virus) => base.OnUpdate(host, virus);

    public override void DoEffect(EntityUid host, VirusComponent virus)
    {
        ApplyBleed(host);
    }

    private void ApplyBleed(EntityUid host)
    {
        if (_entityManager.TryGetComponent<BloodstreamComponent>(host, out var bloodstream))
        {
            var system = _entityManager.System<SharedBloodstreamSystem>();
            system.TryModifyBleedAmount(host, BleedIncrease);
        }
    }

    public override IVirusSymptom Clone()
    {
        return new VascularRuptureSymptom(EffectTimedWindow.Clone());
    }
}