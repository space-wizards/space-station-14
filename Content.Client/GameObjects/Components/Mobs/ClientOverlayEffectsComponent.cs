using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <summary>
    /// A character UI component which shows the current damage state of the mob (living/dead)
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ClientOverlayEffectsComponent : SharedOverlayEffectsComponent//, ICharacterUI
    {
        /// <summary>
        /// An enum representing the current state being applied to the user
        /// </summary>
        private readonly List<string> _currentEffects = new List<string>();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<string> Effects
        {
            get => _currentEffects;
            set => SetEffects(value);
        }

#pragma warning disable 649
        // Required dependencies
        [Dependency] private readonly IOverlayManager _overlayManager;
#pragma warning restore 649

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    SetEffects(Effects);
                    break;
                case PlayerDetachedMsg _:
                    Effects = new List<string>();
                    break;
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is OverlayEffectComponentState state) || Effects.Equals(state.ScreenEffects))
            {
                return;
            }

            Effects = state.ScreenEffects;
        }

        private void SetEffects(List<string> newEffects)
        {
            foreach (var overlayId in Effects.Clone())
            {
                if (!newEffects.Contains(overlayId))
                {
                    RemoveOverlay(overlayId);
                }
            }

            foreach (var overlayId in newEffects)
            {
                if (!Effects.Contains(overlayId))
                {
                    AddOverlay(overlayId);
                }
            }
        }

        private void RemoveOverlay(string overlayId)
        {
            Effects.Remove(overlayId);
            _overlayManager.RemoveOverlay(overlayId);
        }

        private void AddOverlay(string overlayId)
        {
            Effects.Add(overlayId);
            if (TryCreateOverlay(overlayId, out var overlay))
            {
                _overlayManager.AddOverlay(overlay);
            }
            else
            {
                Logger.ErrorS("overlay", $"Could not add overlay {overlayId}");
            }
        }

        private bool TryCreateOverlay(string id, out Overlay overlay)
        {
            var overlayType = Type.GetType($"Content.Client.Graphics.Overlays.{id}", false, true);
            if (overlayType != null)
            {
                overlay = Activator.CreateInstance(overlayType) as Overlay;
                return true;
            }

            overlay = default;
            return false;
        }
    }
}
