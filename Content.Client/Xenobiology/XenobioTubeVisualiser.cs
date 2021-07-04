using System;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Content.Shared.Xenobiology;
using static Content.Shared.Xenobiology.SharedXenoTubeComponent;

namespace Content.Client.Cloning
{
    [UsedImplicitly]
    public class XenobioTubeVisualiser : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(XenoTubeStatus.Powered, out bool powered)) ;// return;
            if (!component.TryGetData(XenoTubeStatus.Occupied, out bool occupied)) ;// return;
            if (!powered) sprite.LayerSetState(0, "tube-unpowered");
            else if (powered)
            {
                if (!occupied) sprite.LayerSetState(0, "tube-powered");
                else if (occupied) sprite.LayerSetState(0, "tube-occupied");
            }
        }
    }
}
