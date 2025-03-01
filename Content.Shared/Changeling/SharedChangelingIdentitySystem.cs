using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

public abstract partial class SharedChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverrideSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
    {
        CloneToNullspace(ent, ent.Owner);
    }

    public void CloneToNullspace(Entity<ChangelingIdentityComponent> ent, EntityUid target)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid)
            || !_prototype.TryIndex(humanoid.Species, out var speciesPrototype)
            || !TryComp<DnaComponent>(target, out var targetDna))
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
    /// <summary>
    /// Simple helper to add a PVS override to a
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="target"></param>
    protected void HandlePvsOverride(EntityUid uid, EntityUid target)
    {
        if(!TryComp<ActorComponent>(uid, out var actor))
            return;

        _pvsOverrideSystem.AddSessionOverride(target, actor.PlayerSession);
    }

    /// <summary>
    /// Inform another Session of the entities stored for Transformation
    /// </summary>
    /// <param name="session">The Session you wish to inform</param>
    /// <param name="comp">The Target storage of identities</param>
    public void HandOverPvsOverride(ICommonSession session, ChangelingIdentityComponent comp)
    {
        foreach (var entity in comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.AddSessionOverride(entity, session);
        }
    }

}

