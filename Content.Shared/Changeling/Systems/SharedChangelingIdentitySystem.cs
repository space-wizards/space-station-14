using System.Numerics;
using Content.Shared.Changeling.Components;
using Content.Shared.Cloning;
using Content.Shared.Humanoid;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Systems;

public abstract class SharedChangelingIdentitySystem : EntitySystem
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingIdentityComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ChangelingIdentityComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ChangelingStoredIdentityComponent, ComponentRemove>(OnStoredRemove);
    }

    private void OnPlayerAttached(Entity<ChangelingIdentityComponent> ent, ref PlayerAttachedEvent args)
    {
        HandOverPvsOverride(ent, args.Player);
    }

    private void OnPlayerDetached(Entity<ChangelingIdentityComponent> ent, ref PlayerDetachedEvent args)
    {
        CleanupPvsOverride(ent, args.Player);
    }

    private void OnMapInit(Entity<ChangelingIdentityComponent> ent, ref MapInitEvent args)
    {
        // Make a backup of our current identity so we can transform back.
        var clone = CloneToPausedMap(ent, ent.Owner);
        ent.Comp.CurrentIdentity = clone;
    }

    private void OnShutdown(Entity<ChangelingIdentityComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<ActorComponent>(ent, out var actor))
            CleanupPvsOverride(ent, actor.PlayerSession);
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
    /// Clone a target humanoid to a paused map.
    /// It creates a perfect copy of the target and can be used to pull components down for future use.
    /// </summary>
    /// <param name="settings">The settings to use for cloning.</param>
    /// <param name="target">The target to clone.</param>
    public EntityUid? CloneToPausedMap(CloningSettingsPrototype settings, EntityUid target)
    {
        // Don't create client side duplicate clones or a clientside map.
        if (_net.IsClient)
            return null;

        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid)
            || !_prototype.Resolve(humanoid.Species, out var speciesPrototype))
            return null;

        EnsurePausedMap();
        var clone = Spawn(speciesPrototype.Prototype, new MapCoordinates(Vector2.Zero, PausedMapId!.Value));

        var storedIdentity = EnsureComp<ChangelingStoredIdentityComponent>(clone);
        storedIdentity.OriginalEntity = target; // TODO: network this once we have WeakEntityReference or the autonetworking source gen is fixed

        if (TryComp<ActorComponent>(target, out var actor))
            storedIdentity.OriginalSession = actor.PlayerSession;

        _humanoidSystem.CloneAppearance(target, clone);
        _cloningSystem.CloneComponents(target, clone, settings);

        var targetName = _nameMod.GetBaseName(target);
        _metaSystem.SetEntityName(clone, targetName);

        return clone;
    }

    /// <summary>
    /// Clone a target humanoid to a paused map and add it to the Changelings list of identities.
    /// It creates a perfect copy of the target and can be used to pull components down for future use.
    /// </summary>
    /// <param name="ent">The Changeling.</param>
    /// <param name="target">The target to clone.</param>
    public EntityUid? CloneToPausedMap(Entity<ChangelingIdentityComponent> ent, EntityUid target)
    {
        if (!_prototype.Resolve(ent.Comp.IdentityCloningSettings, out var settings))
            return null;

        var clone = CloneToPausedMap(settings, target);

        if (clone == null)
            return null;

        ent.Comp.ConsumedIdentities.Add(clone.Value);

        Dirty(ent);
        HandlePvsOverride(ent, clone.Value);

        return clone;
    }

    /// <summary>
    /// Simple helper to add a PVS override to a nullspace identity.
    /// </summary>
    /// <param name="uid">The actor that should get the override.</param>
    /// <param name="identity">The identity stored in nullspace.</param>
    private void HandlePvsOverride(EntityUid uid, EntityUid identity)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _pvsOverrideSystem.AddSessionOverride(identity, actor.PlayerSession);
    }

    /// <summary>
    /// Cleanup all PVS overrides for the owner of the ChangelingIdentity
    /// </summary>
    /// <param name="ent">The changeling storing the identities.</param>
    /// <param name="entityUid"The session you wish to remove the overrides from.</param>
    private void CleanupPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
    {
        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.RemoveSessionOverride(identity, session);
        }
    }

    /// <summary>
    /// Inform another session of the entities stored for transformation.
    /// </summary>
    /// <param name="ent">The changeling storing the identities.</param>
    /// <param name="session">The session you wish to inform.</param>
    public void HandOverPvsOverride(Entity<ChangelingIdentityComponent> ent, ICommonSession session)
    {
        foreach (var identity in ent.Comp.ConsumedIdentities)
        {
            _pvsOverrideSystem.AddSessionOverride(identity, session);
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
