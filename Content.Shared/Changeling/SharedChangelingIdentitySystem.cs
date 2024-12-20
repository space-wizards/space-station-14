using System.Numerics;
using Content.Shared.Changeling.Devour;
using Content.Shared.Changeling.Transform;
using Content.Shared.Forensics;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling;

public abstract partial class SharedChangelingIdentitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _sharedTransformSystem = default!;
    [Dependency] private readonly SharedViewSubscriberSystem _sharedViewSubscriberSystem = default!;

    protected EntityUid? PausedMap { get; private set; }
    public MapId PausedMapId { get; private set; }

    /// <summary>
    /// Initialize the Starting ling entity in nullspace and set the ling as a View Subscriber to the Body to load the PVS
    /// nullspace
    /// </summary>
    /// <param name="uid">The ling to startup</param>
    /// <param name="component">the The ChangelingIdentityComponent attached to the ling</param>
    public void CloneLingStart(EntityUid uid, ChangelingIdentityComponent component)
    {
        if (component.ConsumedIdentities.Count > 0)
            return;
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;
        EntityUid? mob = null;
        CloneToNullspace(uid, component, uid, ref mob);

        _sharedViewSubscriberSystem.AddViewSubscriber(mob!.Value, actor.PlayerSession);
    }

    public void CloneToNullspace(EntityUid uid, ChangelingIdentityComponent component, EntityUid target)
    {
        EntityUid? _ = null;
        CloneToNullspace(uid, component, target, ref _);
    }

    public void CloneToNullspace(EntityUid uid, ChangelingIdentityComponent comp, EntityUid target, ref EntityUid? mobUid)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return; // whatever body was to be cloned, was not a humanoid
        if (!_prototype.TryIndex(humanoid.Species, out var speciesPrototype))
            return;
        if (!TryComp<DnaComponent>(target, out var targetDna))
            return;
        if(!TryComp<TransformComponent>(uid, out var transform))
           return;

        EnsurePausedMap();
        var mob = Spawn(speciesPrototype.Prototype, _sharedTransformSystem.GetMapCoordinates(transform));
        _humanoidSystem.CloneAppearance(target, mob);
        if (!TryComp<DnaComponent>(mob, out var mobDna))
            return;
        if(!TryComp<TransformComponent>(mob, out var mobTransform))
            return;
        mobDna.DNA = targetDna.DNA;
        _metaSystem.SetEntityName(mob, Name(target));
        _metaSystem.SetEntityDescription(mob, MetaData(target).EntityDescription);
        comp.ConsumedIdentities?.Add(mob);
        comp.LastConsumedEntityUid = mob;
        Log.Debug("spawn");
        EntityManager.StartEntity(mob);
        _sharedTransformSystem.SetMapCoordinates((mob,mobTransform), new MapCoordinates(0,0, PausedMapId));
        Dirty(uid, comp);
        mobUid = mob;
    }

    protected void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        PausedMapId = _mapManager.CreateMap();
        _mapManager.SetMapPaused(PausedMapId, true);
        PausedMap = _mapManager.GetMapEntityId(PausedMapId);
    }

}
