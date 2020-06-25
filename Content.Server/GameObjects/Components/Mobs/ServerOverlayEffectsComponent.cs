using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ServerOverlayEffectsComponent : SharedOverlayEffectsComponent
    {
        private readonly List<OverlayContainer> _currentOverlays = new List<OverlayContainer>();

        [ViewVariables(VVAccess.ReadWrite)]
        private List<OverlayContainer> ActiveOverlays => _currentOverlays;

        public override ComponentState GetComponentState()
        {
            return new OverlayEffectComponentState(_currentOverlays);
        }

        public void AddOverlay(OverlayContainer container)
        {
            if (!ActiveOverlays.Contains(container))
            {
                ActiveOverlays.Add(container);
                Dirty();
            }
        }

        public void AddOverlay(string id) => AddOverlay(OverlayContainer.FromID(id));

        public void RemoveOverlay(OverlayContainer container)
        {
            if (ActiveOverlays.Contains(container))
            {
                ActiveOverlays.Remove(container);
                Dirty();
            }
        }

        public void RemoveOverlay(string id) => RemoveOverlay(OverlayContainer.FromID(id));

        public void ClearOverlays()
        {
            ActiveOverlays.Clear();
            Dirty();
        }
    }
}
