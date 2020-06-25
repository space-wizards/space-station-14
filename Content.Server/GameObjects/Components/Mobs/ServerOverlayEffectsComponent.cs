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
        /// Adds overlays. Checks for duplicates.
        /// </summary>
        /// <param name="effects"></param>
        public void AddOverlays(params string[] effects)
        {
            foreach (var effect in effects)
            {
                if (!ActiveOverlays.Contains(effect))
                {
                    ActiveOverlays.Add(effect);
                }
            }

            Dirty();
        }

        /// <summary>
        /// Removes overlays if found
        /// </summary>
        /// <param name="effects"></param>
        public void RemoveOverlays(params string[] effects)
        {
            ActiveOverlays.RemoveAll(effects.Contains);
            Dirty();
        }

        /// <summary>
        /// Sets this to be the only active overlay
        /// </summary>
        /// <param name="effect"></param>
        public void SetOverlay(string effect)
        {
            ClearOverlays();
            ActiveOverlays.Add(effect);
        }

        public void ClearOverlays()
        {
            ActiveOverlays.Clear();
            Dirty();
        }
    }
}
