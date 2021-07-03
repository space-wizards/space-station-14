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
            if (!component.TryGetData(XenoTubeStatus.Powered, out bool powered)) return;
            if (!component.TryGetData(XenoTubeStatus.Occupied, out bool occupied)) return;
            if (powered == false) sprite.LayerSetState(0, "tube-unpowered");
            if (powered == true)
            {
                if (occupied == false) sprite.LayerSetState(0, "tube-powered");
                if (occupied == true) sprite.LayerSetState(0, "tube-occupied");
            }
        }
    }
}
