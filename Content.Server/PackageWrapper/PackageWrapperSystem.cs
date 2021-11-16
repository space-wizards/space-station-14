using System;
using Content.Server.HandLabeler.Components;
using Content.Server.PackageWrapper.Components;
using Content.Shared.HandLabeler;
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
            if (args.Target == null || !args.Target.TryGetComponent(out WrapperTypeComponent? type))
                return;

            component.Owner.PopupMessage(args.User, type.WrapType);

        }
    }
}
