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

        public void AddOverlay(string id) => AddOverlay(new OverlayContainer(id));
        public void AddOverlay(OverlayType type) => AddOverlay(new OverlayContainer(type));

        public void RemoveOverlay(OverlayContainer container)
        {
            if (ActiveOverlays.RemoveAll(c => c.Equals(container)) > 0)
            {
                Dirty();
            }
        }

        public void RemoveOverlay(string id)
        {
            if (ActiveOverlays.RemoveAll(container => container.ID == id) > 0)
            {
                Dirty();
            }
        }

        public void RemoveOverlay(OverlayType type) => RemoveOverlay(type.ToString());

        public void ClearOverlays()
        {
            ActiveOverlays.Clear();
            Dirty();
        }
    }
}
