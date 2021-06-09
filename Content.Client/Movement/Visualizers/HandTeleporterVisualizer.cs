using System;
using Content.Shared.Portal.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Movement.Visualizers
{
    [UsedImplicitly]
    public class HandTeleporterVisualizer : AppearanceVisualizer
    {

        public override void OnChangeData(AppearanceComponent component)
        {
            var sprite = component.Owner.GetComponent<ISpriteComponent>();
            if (!component.TryGetData(TeleporterVisuals.VisualState, out TeleporterVisualState state))
            {
                state = TeleporterVisualState.Ready;
            }

            switch (state)
            {
                case TeleporterVisualState.Charging:
                    sprite.LayerSetState(0, "charging");
                    break;
                case TeleporterVisualState.Ready:
                    sprite.LayerSetState(0, "ready");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

}
