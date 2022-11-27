using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Medical.Wounds.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    private const string WoundContainerId = "WoundSystemWounds";
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;



    public override void Initialize()
    {
        CacheWoundData();
        _prototypeManager.PrototypesReloaded += _ => CacheWoundData();

        SubscribeLocalEvent<BodyComponent, AttackedEvent>(OnBodyAttacked);
    }

    public override void Update(float frameTime)
    {
        UpdateHealing(frameTime);
    }

    private void OnBodyAttacked(EntityUid uid, BodyComponent component, AttackedEvent args)
    {
        if (!TryComp(args.Used, out TraumaInflicterComponent? inflicter))
            return;

        var parts = _body.GetBodyChildren(uid, component).ToList();
        var part = _random.Pick(parts);
        TryApplyTrauma(part.Id, inflicter);
    }
}
