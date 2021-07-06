using Robust.Shared.GameObjects;
using Content.Shared.Xenobiology;
using Content.Server.Power.Components;
using System;
using Robust.Server.GameObjects;

namespace Content.Server.Xenobiology
{
    public class XenobiologyTubeSystem : EntitySystem
    {
       
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpecimenContainmentComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnPowerChanged(EntityUid uid, SpecimenContainmentComponent component, PowerChangedEvent args)
        {
            component.Powered = args.Powered;
            component.UpdateAppearance();
        }
    }
}
