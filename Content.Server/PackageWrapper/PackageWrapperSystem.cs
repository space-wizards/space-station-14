using System;
using Content.Server.PackageWrapper.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.PackageWrapper
{
    [UsedImplicitly]
    public class PackageWrapperSystem : EntitySystem
    {
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
            }
        }
    }
}
