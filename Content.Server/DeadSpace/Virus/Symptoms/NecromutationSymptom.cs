// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus.Symptoms;
using Content.Shared.DeadSpace.Virus.Components;
using Content.Server.DeadSpace.Virus.Systems;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.DeadSpace.Virus.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Virus.Symptoms;

public sealed class NecromutationSymptom : VirusSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    public override VirusSymptom Type => VirusSymptom.Necromutation;
    protected override ProtoId<VirusSymptomPrototype> PrototypeId => "NecromutationSymptom";
    private const string NecroReagent = "ExtractInfectorDead";
    private const float AddAmount = 3f;

    public NecromutationSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow) { }

    public override void OnAdded(EntityUid host, VirusComponent virus)
    {
        base.OnAdded(host, virus);
    }

    public override void OnRemoved(EntityUid host, VirusComponent virus)
    {
        base.OnRemoved(host, virus);
    }

    public override void OnUpdate(EntityUid host, VirusComponent virus)
    {
        base.OnUpdate(host, virus);
    }

    public override void DoEffect(EntityUid host, VirusComponent virus)
    {
        var virusSystem = _entityManager.System<VirusSystem>();
        virusSystem.InfectAround(host);

        if (_entityManager.TryGetComponent<BloodstreamComponent>(host, out var bloodstream)
            && bloodstream.BloodSolution != null)
        {
            var solSystem = _entityManager.System<SharedSolutionContainerSystem>();
            solSystem.TryAddReagent(bloodstream.BloodSolution.Value, NecroReagent, AddAmount, out _);
        }
    }

    public override IVirusSymptom Clone()
    {
        return new NecromutationSymptom(EffectTimedWindow.Clone());
    }
}