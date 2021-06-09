using System;
using Content.Client.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Portal;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.GameObjects.Components.Movement
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
