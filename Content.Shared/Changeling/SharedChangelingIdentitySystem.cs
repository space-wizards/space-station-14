using Content.Shared.Changeling.Devour;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

public sealed class SharedChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;


    public void CloneToNullspace(EntityUid uid, ChangelingIdentityComponent comp, EntityUid target)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return; // whatever body was to be cloned, was not a humanoid
        if (!_prototype.TryIndex(humanoid.Species, out var speciesPrototype))
            return;
        if (!TryComp<DnaComponent>(target, out var targetDna))
            return;


        var mob = Spawn(speciesPrototype.Prototype, MapCoordinates.Nullspace);
        _humanoidSystem.CloneAppearance(target, mob);
        if (!TryComp<DnaComponent>(uid, out var mobDna))
            return;
        mobDna.DNA = targetDna.DNA;
        _metaSystem.SetEntityName(mob, Name(target));
        _metaSystem.SetEntityDescription(uid, MetaData(target).EntityDescription);
        comp.ConsumedIdentities?.Add(mob);
        comp.LastConsumedEntityUid = mob;

        Dirty(uid, comp);
    }
}
