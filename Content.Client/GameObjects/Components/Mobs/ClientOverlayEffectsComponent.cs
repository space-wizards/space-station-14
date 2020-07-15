using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
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
        private List<OverlayContainer> _currentEffects = new List<OverlayContainer>();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<OverlayContainer> ActiveOverlays
        {
            get => _currentEffects;
            set => SetEffects(value);
        }

#pragma warning disable 649
        // Required dependencies
        [Dependency] private readonly IOverlayManager _overlayManager;
        [Dependency] private readonly IReflectionManager _reflectionManager;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    var overlays = new List<OverlayContainer>(_currentEffects);
                    _currentEffects.Clear();
                    SetEffects(overlays);
                    break;
                case PlayerDetachedMsg _:
                    ActiveOverlays = new List<OverlayContainer>();
                    break;
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is OverlayEffectComponentState state))
            {
                return;
            }

            if (_playerManager?.LocalPlayer != null && _playerManager.LocalPlayer.ControlledEntity != Owner)
            {
                _currentEffects = state.Overlays;
                return;
            }

            if (ActiveOverlays.Equals(state.Overlays))
                return;

            ActiveOverlays = state.Overlays;
        }

        private void SetEffects(List<OverlayContainer> newOverlays)
        {
            foreach (var container in ActiveOverlays.ToArray())
            {
                if (!newOverlays.Contains(container))
                {
                    RemoveOverlay(container);
                }
            }

            foreach (var container in newOverlays)
            {
                if (!ActiveOverlays.Contains(container))
                {
                    AddOverlay(container);
                }
            }
        }

        private void RemoveOverlay(OverlayContainer container)
        {
            ActiveOverlays.Remove(container);
            _overlayManager.RemoveOverlay(container.ID);
        }

        private void AddOverlay(OverlayContainer container)
        {
            ActiveOverlays.Add(container);
            if (TryCreateOverlay(container, out var overlay))
            {
                _overlayManager.AddOverlay(overlay);
            }
            else
            {
                Logger.ErrorS("overlay", $"Could not add overlay {container.ID}");
            }
        }

        private bool TryCreateOverlay(OverlayContainer container, out Overlay overlay)
        {
            var overlayTypes = _reflectionManager.GetAllChildren<Overlay>();
            var foundType = overlayTypes.FirstOrDefault(t => t.Name == container.ID);

            if (foundType != null)
            {
                overlay = Activator.CreateInstance(foundType) as Overlay;
                var configurable = foundType
                    .GetInterfaces()
                    .FirstOrDefault(type =>
                        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IConfigurable<>)
                                           && type.GenericTypeArguments.First() == container.GetType());

                if (configurable != null)
                {
                    var method = overlay?.GetType().GetMethod("Configure");
                    method?.Invoke(overlay, new []{ container });
                }

                return true;
            }

            overlay = default;
            return false;
        }
    }
}
