using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

public sealed class ChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverrideSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingIdentityComponent, MindAddedMessage>(OnMindSwapSpell);
        SubscribeLocalEvent<ChangelingIdentityComponent, MindRemovedMessage>(OnMindSwapVictim);
    }

    private void OnMindSwapVictim(Entity<ChangelingIdentityComponent> ent, ref MindRemovedMessage args)
    {
        CleanupPvsOverride(ent, args.Container.Owner);
    }

    private void OnMindSwapSpell(Entity<ChangelingIdentityComponent> ent, ref MindAddedMessage args)
    {
        if(!TryComp<ActorComponent>(args.Container.Owner, out var actor))
            return;
        HandOverPvsOverride(actor.PlayerSession, ent.Comp);
    }

    private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
    {
        CloneToNullspace(ent, ent.Owner);
    }

    private void OnShutdown(Entity<ChangelingIdentityComponent> ent, ref ComponentShutdown args)
    {
        CleanupPvsOverride(ent, ent.Owner);
        CleanupChangelingNullspaceIdentities(ent);
    }

    /// <summary>
    /// Cleanup all nullspaced Identities when the changeling no longer exists
    /// </summary>
    /// <param name="ent">the changeling</param>
    public void CleanupChangelingNullspaceIdentities(Entity<ChangelingIdentityComponent> ent)
    {
        foreach (var consumedIdentity in ent.Comp.ConsumedIdentities)
        {
            QueueDel(consumedIdentity);
        }
    }

    /// <summary>
    /// Clone a target humanoid into nullspace and add it to the Changelings list of identities.
    ///
    /// It creates a perfect copy of the target and can be used to pull components down for future use
    ///
    /// </summary>
    /// <param name="ent">the Changeling</param>
    /// <param name="target">the targets uid</param>
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
    /// Simple helper to add a PVS override to a Nullspace Identity
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="target"></param>
    private void HandlePvsOverride(EntityUid uid, EntityUid target)
    {
        if(!TryComp<ActorComponent>(uid, out var actor))
            return;

        _pvsOverrideSystem.AddSessionOverride(target, actor.PlayerSession);
    }

    /// <summary>
    /// Cleanup all Pvs Overrides for the owner of the ChangelingIdentity
    /// </summary>
    /// <param name="ent">the Changeling itself</param>
    /// <param name="entityUid">Who specifically to cleanup from, usually just the same owner, but in the case of a mindswap we want to clean up the victim</param>
    private void CleanupPvsOverride(Entity<ChangelingIdentityComponent> ent, EntityUid entityUid)
    {
        if(!TryComp<ActorComponent>(entityUid, out var actor))
            return;

        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.RemoveSessionOverride(identity, actor.PlayerSession);
        }
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
