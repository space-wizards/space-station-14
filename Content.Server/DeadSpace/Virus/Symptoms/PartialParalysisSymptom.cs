// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus.Symptoms;
using Content.Shared.DeadSpace.Virus.Components;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Virus.Symptoms;

public sealed class PartialParalysisSymptom : VirusSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    public override VirusSymptom Type => VirusSymptom.PartialParalysis;
    protected override ProtoId<VirusSymptomPrototype> PrototypeId => "PartialParalysisSymptom";

    public PartialParalysisSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow) { }

    public override void OnAdded(EntityUid host, VirusComponent virus)
    {
        base.OnAdded(host, virus);
        _entityManager.EnsureComponent<PartialParalysisComponent>(host);
        _entityManager.System<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(host);
    }

    public override void OnRemoved(EntityUid host, VirusComponent virus)
    {
        base.OnRemoved(host, virus);
        _entityManager.RemoveComponent<PartialParalysisComponent>(host);
        _entityManager.System<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(host);
    }

    public override void OnUpdate(EntityUid host, VirusComponent virus) => base.OnUpdate(host, virus);
    public override void DoEffect(EntityUid host, VirusComponent virus) { }
    public override IVirusSymptom Clone() => new PartialParalysisSymptom(EffectTimedWindow.Clone());
}
