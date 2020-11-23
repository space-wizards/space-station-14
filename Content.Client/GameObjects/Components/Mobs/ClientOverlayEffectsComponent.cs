using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Client.GameObjects;
using Robust.Client.Graphics.Overlays;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
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
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;

        /// <summary>
        /// A list of overlay containers representing the current overlays applied
        /// </summary>
        private List<OverlayContainer> _currentEffects = new List<OverlayContainer>();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<OverlayContainer> ActiveOverlays
        {
            get => _currentEffects;
            set => SetEffects(value);
        }

        public override void Initialize()
        {
            base.Initialize();

            UpdateOverlays();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    UpdateOverlays();
                    break;
                case PlayerDetachedMsg _:
                    ActiveOverlays.ForEach(o => _overlayManager.RemoveOverlay(o.ID));
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            if (message is OverlayEffectComponentMessage overlayMessage)
            {
                SetEffects(overlayMessage.Overlays);
            }
        }

        private void UpdateOverlays()
        {
            _currentEffects = _overlayManager.AllOverlays
                .Where(overlay => Enum.IsDefined(typeof(SharedOverlayID), overlay.ID))
                .Select(overlay => new OverlayContainer(overlay.ID))
                .ToList();

            foreach (var overlayContainer in ActiveOverlays)
            {
                if (!_overlayManager.HasOverlay(overlayContainer.ID))
                {
                    if (TryCreateOverlay(overlayContainer, out var overlay))
                    {
                        _overlayManager.AddOverlay(overlay);
                    }
                }
            }

            SendNetworkMessage(new ResendOverlaysMessage(), _netManager.ServerChannel);
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
                else
                {
                    UpdateOverlayConfiguration(container, _overlayManager.GetOverlay(container.ID));
                }
            }

            _currentEffects = newOverlays;
        }

        private void RemoveOverlay(OverlayContainer container)
        {
            ActiveOverlays.Remove(container);
            _overlayManager.RemoveOverlay(container.ID);
        }

        private void AddOverlay(OverlayContainer container)
        {
            if (_overlayManager.HasOverlay(container.ID))
            {
                return;
            }

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

        private void UpdateOverlayConfiguration(OverlayContainer container, Overlay overlay)
        {
            if (overlay is IConfigurableOverlay configurable)
            {
                foreach (var param in container.Parameters)
                {
                    configurable.Configure(param);
                }
            }
        }

        private bool TryCreateOverlay(OverlayContainer container, out Overlay overlay)
        {
            var overlayTypes = _reflectionManager.GetAllChildren<Overlay>();
            var overlayType = overlayTypes.FirstOrDefault(t => t.Name == container.ID);

            if (overlayType != null)
            {
                overlay = IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance<Overlay>(overlayType);
                UpdateOverlayConfiguration(container, overlay);
                return true;
            }

            overlay = default;
            return false;
        }
    }
}
