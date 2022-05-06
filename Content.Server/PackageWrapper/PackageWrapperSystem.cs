using System.Linq;
using Content.Server.PackageWrapper.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Content.Server.Storage.Components;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Utility;


namespace Content.Server.PackageWrapper
{
    [UsedImplicitly]
    public class PackageWrapSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PackageWrapperComponent, AfterInteractEvent>(AfterInteractOn);
        }

        private void AfterInteractOn(EntityUid uid, PackageWrapperComponent component, AfterInteractEvent args)
        {
            if (args.Target != null)
            {
                //Don't wrap anchored items
                if (TryComp<TransformComponent>(args.Target.Value, out var targetTransform))
                {
                    if (targetTransform.Anchored)
                    {
                        return;
                    }
                }
                //Don't wrap wrapped items
                if (TryComp<WrappedStorageComponent>(args.Target, out _))
                {
                    component.Owner.PopupMessage(args.User, Loc.GetString("on-failed-wrap-message",
                        ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName)));
                    return;
                }


                // If object shaped, wrap it in shape
                if(TryComp<WrappableInShapeComponent>(args.Target.Value, out var targetWrapType))
                {
                    var typeComp = component.ProductsShaped.FirstOrDefault(x => targetWrapType.WrapType == x.ID);

                    if(TryComp<EntityStorageComponent>(args.Target.Value, out var targetStorage))
                    {
                        // Cannot wrap the entity is currently opened.
                        if (targetStorage.Open)
                            return;
                    }

                    if (typeComp != null)
                    {
                        var spawnedObj = Spawn(typeComp.ProtoSpawnID, Comp<TransformComponent>((EntityUid) args.Target).Coordinates);
                        var container = Comp<WrappedStorageComponent>(spawnedObj);
                        container.ItemContainer.Insert(args.Target.Value);

                        //There must be better way of getting entity name
                        component.Owner.PopupMessage(args.User,
                            Loc.GetString("on-successful-wrap-message",
                                ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName),
                                ("result", Comp<MetaDataComponent>(spawnedObj).EntityName)));
                    }
                }
                // If object not shaped, wrap by it's size
                else if (TryComp<SharedItemComponent>(args.Target.Value, out var targetItem))
                {
                    // find minimal size
                    component.Products
                        .OrderBy(w => w.Capacity)
                        .TryFirstOrDefault(item => targetItem.Size <= item.Capacity, out var typeComp);
                    // Spawn item with minimal size and insert
                    if (typeComp != null)
                    {
                        EntityUid spawnedObjectUid;

                        if (_containerSystem.TryGetContainingContainer(args.Target.Value, out var itemContainer))
                        {
                            // Drop item
                            Transform(args.Target.Value).AttachToGridOrMap();
                            var spawnedWrapperContainer = Spawn(typeComp.ProtoSpawnID,Transform(args.Target.Value).MapPosition);
                            var wrapperComp = Comp<WrappedStorageComponent>(spawnedWrapperContainer);

                            itemContainer.Insert(wrapperComp.Owner);
                            wrapperComp.ItemContainer.Insert(args.Target.Value);

                            spawnedObjectUid = spawnedWrapperContainer;
                        }
                        else
                        {
                            //Spawn in coords
                            var spawnedWrapperContainer = Spawn(typeComp.ProtoSpawnID, Transform(args.Target.Value).Coordinates);
                            var wrapperComp = Comp<WrappedStorageComponent>(spawnedWrapperContainer);
                            wrapperComp.ItemContainer.Insert(args.Target.Value);

                            spawnedObjectUid = spawnedWrapperContainer;
                        }

                        component.Owner.PopupMessage(args.User,
                            Loc.GetString("on-successful-wrap-message",
                                ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName),
                                ("result", Comp<MetaDataComponent>(spawnedObjectUid).EntityName)));
                    }
                }
            }
        }
    }
}
