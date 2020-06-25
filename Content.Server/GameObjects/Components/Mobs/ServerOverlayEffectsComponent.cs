using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ServerOverlayEffectsComponent : SharedOverlayEffectsComponent
    {
        private readonly List<string> _currentOverlays = new List<string>();

        [ViewVariables(VVAccess.ReadWrite)]
        private List<string> ActiveOverlays => _currentOverlays;

        public override ComponentState GetComponentState()
        {
            return new OverlayEffectComponentState(_currentOverlays);
        }

        /// <summary>
        /// Adds overlays based on their ID
        /// </summary>
        /// <param name="overlayIds"></param>
        public void AddOverlays(params string[] overlayIds)
        {
            foreach (var overlayId in overlayIds)
            {
                if (!ActiveOverlays.Contains(overlayId))
                {
                    ActiveOverlays.Add(overlayId);
                }
            }

            Dirty();
        }

        /// <summary>
        /// Removes overlays
        /// </summary>
        /// <param name="overlayIds"></param>
        public void RemoveOverlays(params string[] overlayIds)
        {
            ActiveOverlays.RemoveAll(overlayIds.Contains);
            Dirty();
        }

        /// <summary>
        /// Sets this to be the only active overlay
        /// </summary>
        /// <param name="overlayId"></param>
        public void SetOverlay(string overlayId)
        {
            ClearOverlays();
            ActiveOverlays.Add(overlayId);
        }

        public void ClearOverlays()
        {
            ActiveOverlays.Clear();
            Dirty();
        }
    }
}
