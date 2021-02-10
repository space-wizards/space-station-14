using System;
using Content.Shared.GameObjects.Components.Singularity;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.GameObjects.Components.Singularity
{
    [UsedImplicitly]
    public class EmitterVisualizer : AppearanceVisualizer
    {
        private const string OverlayBeam = "emitter-beam";
        private const string OverlayUnderPowered = "emitter-underpowered";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            if (!component.TryGetData(EmitterVisuals.Locked, out bool locked))
                locked = false;


            if (!component.TryGetData(EmitterVisuals.VisualState, out EmitterVisualState state))
                state = EmitterVisualState.Off;

            switch (state)
            {
                case EmitterVisualState.On:
                    sprite.LayerSetVisible(1, true);
                    sprite.LayerSetState(1, OverlayBeam);
                    break;
                case EmitterVisualState.Underpowered:
                    sprite.LayerSetVisible(1, true);
                    sprite.LayerSetState(1, OverlayUnderPowered);
                    break;
                case EmitterVisualState.Off:
                    sprite.LayerSetVisible(1, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            sprite.LayerSetVisible(2, locked);
        }
    }
}
