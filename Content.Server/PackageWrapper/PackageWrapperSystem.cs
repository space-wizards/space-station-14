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
                if(TryComp<WrapableShapeComponent>(args.Target.Value, out var targetWrapType)) // Broken!
                {
                    var typeComp = component.ProductsShaped.FirstOrDefault(x => targetWrapType.WrapType == x.ID);

                    if (typeComp != null)
                    {
                        var spawnedObj2 = Spawn(typeComp.ProtoSpawnID, Comp<TransformComponent>((EntityUid) args.Target).MapPosition);
                        var container2 = Comp<WrappedStorageComponent>(spawnedObj2);
                        container2.ItemContainer.Insert(args.Target.Value);
                    }
                }
                else if (TryComp<SharedItemComponent>(args.Target.Value, out var targetItem))
                {
                    // find minimal size
                    var typeComp = component.Products
                        .OrderBy(w => w.Capacity)
                        .FirstOrDefault(item => targetItem.Size <= item.Capacity);
                    // Spawn item with minimal size and insert
                    if (typeComp != null)
                    {
                        var spawnedObj2 = Spawn(typeComp.ProtoSpawnID, Comp<TransformComponent>((EntityUid) args.Target).MapPosition);
                        var container2 = Comp<WrappedStorageComponent>(spawnedObj2);
                        container2.ItemContainer.Insert(args.Target.Value);
                    }
                }
                else
                {
                    component.Owner.PopupMessage(args.User, "can't wrap"); // TODO: Add more popup messages like I can't fit this object, etc
                }
            }
        }
    }
}
