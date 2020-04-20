using System.Collections.Generic;
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
        private ScreenEffects _currentEffect = ScreenEffects.None;

#pragma warning disable 649
        // Required dependencies
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        /// <summary>
        /// Holds the screen effects that can be applied mapped ot their relevant overlay
        /// </summary>
        private Dictionary<ScreenEffects, Overlay> _effectsDictionary;

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        private bool CurrentlyControlled => _playerManager.LocalPlayer.ControlledEntity == Owner;

        public override void OnAdd()
        {
            base.OnAdd();

            _effectsDictionary = new Dictionary<ScreenEffects, Overlay>()
            {
                { ScreenEffects.CircleMask, new CircleMaskOverlay() },
                { ScreenEffects.GradientCircleMask, new GradientCircleMask() }
            };
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    SetOverlay(_currentEffect);
                    break;
                case PlayerDetachedMsg _:
                    RemoveOverlay();
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    SetOverlay(_currentEffect);
                    break;
                case PlayerDetachedMsg _:
                    RemoveOverlay();
                    break;
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is OverlayEffectComponentState state) || _currentEffect == state.ScreenEffect) return;
            SetOverlay(state.ScreenEffect);
        }

        private void SetOverlay(ScreenEffects effect)
        {
            RemoveOverlay();

            _currentEffect = effect;

            ApplyOverlay();
        }

        private void RemoveOverlay()
        {
            if (CurrentlyControlled && _currentEffect != ScreenEffects.None)
            {
                var appliedEffect = _effectsDictionary[_currentEffect];
                _overlayManager.RemoveOverlay(appliedEffect.ID);
            }

            _currentEffect = ScreenEffects.None;
        }

        private void ApplyOverlay()
        {
            if (CurrentlyControlled && _currentEffect != ScreenEffects.None)
            {
                var overlay = _effectsDictionary[_currentEffect];
                if (_overlayManager.HasOverlay(overlay.ID))
                {
                    return;
                }
                _overlayManager.AddOverlay(overlay);
                Logger.InfoS("overlay", $"Changed overlay to {overlay}");
            }
        }
    }
}
