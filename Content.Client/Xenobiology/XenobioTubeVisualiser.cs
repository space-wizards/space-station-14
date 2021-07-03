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
            if (!component.TryGetData(XenoTubeStatus.Powered, out XenoTubeStatus powered)) return;
            if (!component.TryGetData(XenoTubeStatus.Occupied, out XenoTubeStatus occupied)) return;
            if (powered.Equals(false)) sprite.LayerSetState(0, "tube-unpowered");
            if (powered.Equals(true))
            {
                if (occupied.Equals(false)) sprite.LayerSetState(0, "tube-powered");
                if (occupied.Equals(true)) sprite.LayerSetState(0, "tube-occupied");
            }
        }
    }
}
