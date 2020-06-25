using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Graphics.Overlays;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Client.GameObjects
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
        private string[] _currentEffects = new string[0];

        private string[] Effects
        {
            get => _currentEffects;
            set
            {
                _currentEffects = value;
                UpdateEffects();
            }
        }


#pragma warning disable 649
        // Required dependencies
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        private bool CurrentlyControlled => _playerManager.LocalPlayer.ControlledEntity == Owner;

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    UpdateEffects();
                    break;
                case PlayerDetachedMsg _:
                    Effects = new string[0];
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

        private void UpdateEffects()
        {
            foreach (var overlay in _overlayManager.AllOverlays)
            {
                if (!Effects.Contains(overlay.ID))
                {
                    _overlayManager.RemoveOverlay(overlay.ID);
                }
            }

            foreach (var overlayId in Effects)
            {
                if (!_overlayManager.HasOverlay(overlayId))
                {
                    if (TryCreateOverlay(overlayId, out var overlay))
                    {
                        _overlayManager.AddOverlay(overlay);
                    }
                    else
                    {
                        Logger.ErrorS("overlay", $"Could not add overlay {overlayId}");
                    }
                }
            }
        }

        private bool TryCreateOverlay(string id, out Overlay overlay)
        {
            var overlayType = Type.GetType(id, false);
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
