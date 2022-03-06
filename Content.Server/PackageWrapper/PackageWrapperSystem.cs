using System;
using Content.Server.PackageWrapper.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

using Content.Server.Electrocution;
using Content.Server.Power.Components;
using Content.Server.Tools;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

using Robust.Shared.Containers;


namespace Content.Server.PackageWrapper
{
    [UsedImplicitly]
    public class PackageWrapperSystem : EntitySystem
    {
        public Container? Storage;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PackageWrapperComponent, AfterInteractEvent>(AfterInteractOn);
        }

        private void AfterInteractOn(EntityUid uid, PackageWrapperComponent component, AfterInteractEvent args)
        {

            if (args.Target is not {Valid: true} target && TryComp<WrappableComponent>(args.Target,out var wrappable))
            {
                component.Owner.PopupMessage(args.User, wrappable.WrapType);

                Spawn(component.CableDroppedOnCutPrototype, Transform(uid).Coordinates); // Спавним контейнер на позицию объекта
                QueueDel(uid);
                    //if (Storage != null) Storage.Insert(target); // Через Insert вставляем внутрь объект на который нажали
            }
        }
    }
}
