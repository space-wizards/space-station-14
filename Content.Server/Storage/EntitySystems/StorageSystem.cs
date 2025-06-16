using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Cloning.Events;
using Content.Shared.Explosion;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Lock;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem : SharedStorageSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StorageComponent, BeforeExplodeEvent>(OnExploded);
        SubscribeLocalEvent<StorageComponent, CloningEvent>(OnClone);

        SubscribeLocalEvent<StorageFillComponent, MapInitEvent>(OnStorageFillMapInit);

    }

    private void OnClone(Entity<StorageComponent> ent, ref CloningEvent args)
    {
        var cloneStorageComponent = EnsureComp<StorageComponent>(args.CloneUid);
        cloneStorageComponent.Grid = ent.Comp.Grid;
        cloneStorageComponent.MaxItemSize = ent.Comp.MaxItemSize;
        cloneStorageComponent.StorageOpenSound = ent.Comp.StorageOpenSound;
        cloneStorageComponent.StorageCloseSound = ent.Comp.StorageCloseSound;

        var cloneUserInterfaceComponent = EnsureComp<UserInterfaceComponent>(args.CloneUid);

        _userInterface.SetUi((args.CloneUid, cloneUserInterfaceComponent), StorageComponent.StorageUiKey.Key, new InterfaceData("StorageBoundUserInterface"));
    }

    private void OnExploded(Entity<StorageComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.Container.ContainedEntities);
    }

    /// <inheritdoc />
    public override void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates, EntityCoordinates finalCoordinates,
        Angle initialRotation, EntityUid? user = null)
    {
        var filter = Filter.Pvs(uid).RemoveWhereAttachedEntity(e => e == user);
        RaiseNetworkEvent(new PickupAnimationEvent(GetNetEntity(uid), GetNetCoordinates(initialCoordinates), GetNetCoordinates(finalCoordinates), initialRotation), filter);
    }
}
