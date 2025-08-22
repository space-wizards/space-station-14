using System.Numerics;
using Content.Shared.Cloning;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

public sealed class ChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;
    [Dependency] private readonly SharedCloningSystem _cloningSystem = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverrideSystem = default!;

    public MapId? PausedMapId;
    private int _numberOfStoredIdentities = 0; // TODO: remove this

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingIdentityComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ChangelingIdentityComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ChangelingStoredIdentityComponent, ComponentRemove>(OnStoredRemove);
    }

    private void OnMindAdded(Entity<ChangelingIdentityComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<ActorComponent>(args.Container.Owner, out var actor))
            return;

        HandOverPvsOverride(actor.PlayerSession, ent.Comp);
    }

    private void OnMindRemoved(Entity<ChangelingIdentityComponent> ent, ref MindRemovedMessage args)
    {
        CleanupPvsOverride(ent, args.Container.Owner);
    }

    private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
    {
        // Make a backup of our current identity so we can transform back.
        var clone = CloneToPausedMap(ent, ent.Owner);
        ent.Comp.CurrentIdentity = clone;
    }

    private void OnShutdown(Entity<ChangelingIdentityComponent> ent, ref ComponentShutdown args)
    {
        CleanupPvsOverride(ent, ent.Owner);
        CleanupChangelingNullspaceIdentities(ent);
    }

    private void OnStoredRemove(Entity<ChangelingStoredIdentityComponent> ent, ref ComponentRemove args)
    {
        // The last stored identity is being deleted, we can clean up the map.
        if (_net.IsServer && PausedMapId != null && Count<ChangelingStoredIdentityComponent>() <= 1)
            _map.QueueDeleteMap(PausedMapId.Value);
    }

    /// <summary>
    /// Cleanup all nullspaced Identities when the changeling no longer exists
    /// </summary>
    /// <param name="ent">the changeling</param>
    public void CleanupChangelingNullspaceIdentities(Entity<ChangelingIdentityComponent> ent)
    {
        if (_net.IsClient)
            return;

        foreach (var consumedIdentity in ent.Comp.ConsumedIdentities)
        {
            QueueDel(consumedIdentity);
        }
    }

    /// <summary>
    /// Clone a target humanoid into nullspace and add it to the Changelings list of identities.
    /// It creates a perfect copy of the target and can be used to pull components down for future use
    /// </summary>
    /// <param name="ent">the Changeling</param>
    /// <param name="target">the targets uid</param>
    public EntityUid? CloneToPausedMap(Entity<ChangelingIdentityComponent> ent, EntityUid target)
    {
        // Don't create client side duplicate clones or a clientside map.
        if (_net.IsClient)
            return null;

        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid)
            || !_prototype.Resolve(humanoid.Species, out var speciesPrototype)
            || !_prototype.Resolve(ent.Comp.IdentityCloningSettings, out var settings))
            return null;

        EnsurePausedMap();
        // TODO: Setting the spawn location is a shitty bandaid to prevent admins from crashing our servers.
        // Movercontrollers and mob collisions are currently being calculated even for paused entities.
        // Spawning all of them in the same spot causes severe performance problems.
        // Cryopods and Polymorph have the same problem.
        var mob = Spawn(speciesPrototype.Prototype, new MapCoordinates(new Vector2(2 * _numberOfStoredIdentities++, 0), PausedMapId!.Value));

        var storedIdentity = EnsureComp<ChangelingStoredIdentityComponent>(mob);
        storedIdentity.OriginalEntity = target; // TODO: network this once we have WeakEntityReference or the autonetworking source gen is fixed

        if (TryComp<ActorComponent>(target, out var actor))
            storedIdentity.OriginalSession = actor.PlayerSession;

        _humanoidSystem.CloneAppearance(target, mob);
        _cloningSystem.CloneComponents(target, mob, settings);

        var targetName = _nameMod.GetBaseName(target);
        _metaSystem.SetEntityName(mob, targetName);
        ent.Comp.ConsumedIdentities.Add(mob);

        Dirty(ent);
        HandlePvsOverride(ent, mob);

        return mob;
    }

    /// <summary>
    /// Simple helper to add a PVS override to a Nullspace Identity
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="target"></param>
    private void HandlePvsOverride(EntityUid uid, EntityUid target)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
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
        if (!TryComp<ActorComponent>(entityUid, out var actor))
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

    /// <summary>
    /// Create a paused map for storing devoured identities as a clone of the player.
    /// </summary>
    private void EnsurePausedMap()
    {
        if (_map.MapExists(PausedMapId))
            return;

        var mapUid = _map.CreateMap(out var newMapId);
        _metaSystem.SetEntityName(mapUid, Loc.GetString("changeling-paused-map-name"));
        PausedMapId = newMapId;
        _map.SetPaused(mapUid, true);
    }
}
