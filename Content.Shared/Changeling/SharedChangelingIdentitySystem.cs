using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

public abstract partial class SharedChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out _))
            return;
        if (_roles.MindHasRole<ChangelingRoleComponent>(mindId))
            return;
        _roles.MindAddRole(mindId, "MindRoleChangeling");
    }

    /// <summary>
    /// Initialize the Starting ling entity in nullspace and set the ling as a View Subscriber to the Body to load the PVS
    /// nullspace
    /// </summary>
    /// <param name="ent">The ling to startup</param>
    public void CloneLingStart(Entity<ChangelingIdentityComponent> ent)
    {
        if (ent.Comp.ConsumedIdentities.Count > 0)
            return;

        CloneToNullspace(ent, ent.Owner);
    }

    public void CloneToNullspace(Entity<ChangelingIdentityComponent> ent, EntityUid target)
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
        ent.Comp.ConsumedIdentities.Add(mob);

        ent.Comp.LastConsumedEntityUid = mob;

        SetPaused(mob, true);
        Dirty(ent);
        HandlePvsOverride(ent, mob);
    }

    protected virtual void HandlePvsOverride(EntityUid uid, EntityUid target) { }

}

