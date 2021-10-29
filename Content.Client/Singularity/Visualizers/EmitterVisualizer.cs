using System;
using Content.Shared.Singularity.Components;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Singularity.Visualizers
{
    [UsedImplicitly]
    public class EmitterVisualizer : AppearanceVisualizer
    {
        private const string OverlayBeam = "beam";
        private const string OverlayUnderPowered = "underpowered";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
            {
                return;
            }

            if (!component.TryGetData(StorageVisuals.Locked, out bool locked))
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
