using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.Xenobiology;
using static Content.Shared.Xenobiology.SharedXenoTubeComponent;

namespace Content.Client.Xenobiology
{
    [UsedImplicitly]
    public class XenobioTubeVisualiser : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            component.Owner.TryGetComponent<ISpriteComponent>(out ISpriteComponent? spritecomp);
            if (spritecomp != null)
            {
                if (!component.TryGetData(XenoTubeStatus.Powered, out bool powered)) return;
                if (!component.TryGetData(XenoTubeStatus.Occupied, out bool occupied)) return;
                if (!powered) spritecomp.LayerSetState(0, "tube-unpowered");
                else if (powered)
                {
                    if (!occupied) spritecomp.LayerSetState(0, "tube-powered");
                    else spritecomp.LayerSetState(0, "tube-occupied");
                }
            }
        }
    }
}
