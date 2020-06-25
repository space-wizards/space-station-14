using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ServerOverlayEffectsComponent : SharedOverlayEffectsComponent
    {
        private List<string> _currentOverlays = new List<string>();

        public List<string> ActiveOverlays
        {
            get => _currentOverlays;
            private set
            {
                _currentOverlays = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new OverlayEffectComponentState(_currentOverlays.ToArray());
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
        }

        /// <summary>
        /// Removes overlays if found
        /// </summary>
        /// <param name="effects"></param>
        public void RemoveOverlays(params string[] effects)
        {
            ActiveOverlays.RemoveAll(effects.Contains);
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
        }
    }
}
