// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Virus.Symptoms;
using Content.Shared.DeadSpace.Virus.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeadSpace.TimeWindow;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.DeadSpace.Virus.Prototypes;

namespace Content.Server.DeadSpace.Virus.Symptoms;

public sealed class EnhancedNecrosisSymptom : VirusSymptomBase
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override VirusSymptom Type => VirusSymptom.EnhancedNecrosis;
    protected override ProtoId<VirusSymptomPrototype> PrototypeId => "EnhancedNecrosisSymptom";
    private static readonly ProtoId<DamageTypePrototype> NecrosisDamageType = "Cellular";
    private const float DamageAmount = 6f;

    public EnhancedNecrosisSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow) { }

    public override void OnAdded(EntityUid host, VirusComponent virus)
    {
        base.OnAdded(host, virus);
        _entityManager.EnsureComponent<RotAccelerationComponent>(host);
    }

    public override void OnRemoved(EntityUid host, VirusComponent virus)
    {
        base.OnRemoved(host, virus);
        _entityManager.RemoveComponent<RotAccelerationComponent>(host);
    }

    public override void OnUpdate(EntityUid host, VirusComponent virus)
    {
        base.OnUpdate(host, virus);
    }

    public override void DoEffect(EntityUid host, VirusComponent virus)
    {
        var damageableSystem = _entityManager.System<DamageableSystem>();
        var popupSystem = _entityManager.System<PopupSystem>();

        DamageSpecifier dspec = new();
        dspec.DamageDict.Add(NecrosisDamageType, DamageAmount);

        damageableSystem.TryChangeDamage(host, dspec, true);

        var messageKey = _random.Pick(new[]
        {
            "virus-necrosis-popup-1",
            "virus-necrosis-popup-2",
            "virus-necrosis-popup-3",
            "virus-necrosis-popup-4",
            "virus-necrosis-popup-5"
        });

        popupSystem.PopupEntity(Loc.GetString(messageKey), host, host, PopupType.Medium);
    }

    public override void ApplyDataEffect(VirusData data, bool add)
    {
        base.ApplyDataEffect(data, add);
        if (add)
            data.DamageWhenDead += 2f;
        else
            data.DamageWhenDead -= 2f;
    }

    public override IVirusSymptom Clone()
    {
        return new EnhancedNecrosisSymptom(EffectTimedWindow.Clone());
    }
}
