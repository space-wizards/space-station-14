using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

public abstract partial class SharedChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;

    /// <summary>
    /// Initialize the Starting ling entity in nullspace and set the ling as a View Subscriber to the Body to load the PVS
    /// nullspace
    /// </summary>
    /// <param name="uid">The ling to startup</param>
    /// <param name="component">The ChangelingIdentityComponent attached to the ling</param>
    public void CloneLingStart(EntityUid uid, ChangelingIdentityComponent component)
    {
        if (component.ConsumedIdentities.Count > 0)
            return;

        CloneToNullspace(uid, component, uid);
    }

    public void CloneToNullspace(EntityUid uid, ChangelingIdentityComponent comp, EntityUid target)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid)
            || !_prototype.TryIndex(humanoid.Species, out var speciesPrototype)
            ||!TryComp<DnaComponent>(target, out var targetDna))
            return;

        var mob = Spawn(speciesPrototype.Prototype, MapCoordinates.Nullspace);

        _humanoidSystem.CloneAppearance(target, mob);

        if (!TryComp<DnaComponent>(mob, out var mobDna))
            return;

        mobDna.DNA = targetDna.DNA;

        _metaSystem.SetEntityName(mob, Name(target));
        _metaSystem.SetEntityDescription(mob, MetaData(target).EntityDescription);
        comp.ConsumedIdentities.Add(mob);

        comp.LastConsumedEntityUid = mob;

        SetPaused(mob, true);
        Dirty(uid, comp);
        HandlePvsOverride(uid, mob);
    }

    protected virtual void HandlePvsOverride(EntityUid uid, EntityUid target) { }

}
