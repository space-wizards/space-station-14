using System.Linq;
using Content.Server.PackageWrapper.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Content.Server.Storage.Components;
using Content.Shared.Item;
using Robust.Shared.Utility;


namespace Content.Server.PackageWrapper
{
    [UsedImplicitly]
    public class PackageWrapSystem : EntitySystem
    {
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
                        component.Owner.PopupMessage(args.User, Loc.GetString("on-failed-wrap-message",
                            ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName)));
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
                if(TryComp<WrapableShapeComponent>(args.Target.Value, out var targetWrapType))
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
                        var spawnedObj = Spawn(typeComp.ProtoSpawnID, Comp<TransformComponent>((EntityUid) args.Target).MapPosition);
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
                        var spawnedObj = Spawn(typeComp.ProtoSpawnID, Comp<TransformComponent>((EntityUid) args.Target).MapPosition);
                        var container = Comp<WrappedStorageComponent>(spawnedObj);
                        container.ItemContainer.Insert(args.Target.Value);

                        component.Owner.PopupMessage(args.User,
                            Loc.GetString("on-successful-wrap-message",
                                ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName),
                                ("result", Comp<MetaDataComponent>(spawnedObj).EntityName)));
                    }
                    else
                    {
                        component.Owner.PopupMessage(args.User, Loc.GetString("on-failed-wrap-size-message",
                            ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName)));
                    }
                }
                else
                {
                    component.Owner.PopupMessage(args.User, Loc.GetString("on-failed-wrap-message",
                        ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName)));
                }
            }
        }
    }
}
