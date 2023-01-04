using System;
using Content.Shared.Singularity.Components;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Singularity.Visualizers
{
    [UsedImplicitly]
    public sealed class EmitterVisualizer : AppearanceVisualizer
    {
        private const string OverlayBeam = "beam";
        private const string OverlayUnderPowered = "underpowered";
        private const string OverlayLocked = "lock"; // Corvax-Resprite
        private const string OverlayUnlocked = "unlock"; // Corvax-Resprite

        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
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

            sprite.LayerSetState(2, locked ? OverlayLocked : OverlayUnlocked); // Corvax-Resprite
        }
    }
}
