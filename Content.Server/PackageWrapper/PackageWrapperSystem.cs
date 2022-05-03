using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.PackageWrapper.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Tools;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.VendingMachines;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
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
                if (TryComp<TransformComponent>(args.Target.Value, out var targetTransform))
                {
                    if (targetTransform.Anchored)
                    {
                        component.Owner.PopupMessage(args.User, Loc.GetString("on-failed-wrap-verb-message",
                            ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName)));
                        return;
                    }
                }

                if(TryComp<WrapableShapeComponent>(args.Target.Value, out var targetWrapType)) // Broken!
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
                            Loc.GetString("on-successful-wrap-verb-message",
                                ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName),
                                ("result", Comp<MetaDataComponent>(spawnedObj).EntityName)));
                    }
                }
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
                            Loc.GetString("on-successful-wrap-verb-message",
                                ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName),
                                ("result", Comp<MetaDataComponent>(spawnedObj).EntityName)));
                    }
                    else
                    {
                        component.Owner.PopupMessage(args.User, Loc.GetString("on-failed-wrap-size-verb-message",
                            ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName)));
                    }
                }
                else
                {
                    component.Owner.PopupMessage(args.User, Loc.GetString("on-failed-wrap-verb-message",
                        ("target", Comp<MetaDataComponent>(args.Target.Value).EntityName)));
                }
            }
        }
    }
}
