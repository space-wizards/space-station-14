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

    [RegisterComponent]
    [ComponentReference(typeof(SharedOverlayEffectsComponent))]
    public sealed class ClientOverlayEffectsComponent : SharedOverlayEffectsComponent//, ICharacterUI
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IClientNetManager _netManager = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public List<OverlayContainer> ActiveOverlays { get; private set; } = new List<OverlayContainer>();

        public override void Initialize()
        {
            base.Initialize();
            AttachOverlaysToManager();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            switch (message)
            {
                case PlayerAttachedMsg _:
                    AttachOverlaysToManager();
                    break;
                case PlayerDetachedMsg _:
                    DetachOverlaysFromManager();
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            switch (message)
            {
                case OverlayEffectsSyncMessage msg:
                    SetOverlays(msg.Overlays);
                    break;
                case OverlayEffectsUpdateMessage msg:
                    UpdateOverlay(msg.ID, msg.Parameters);
                    break;
            }
        }






        /// <summary>
        ///     Adds all overlays in <see cref="ActiveOverlays"/> to <see cref="IOverlayManager"/> that are not already active.
        /// </summary>
        private void AttachOverlaysToManager()
        {
            foreach (var overlayContainer in ActiveOverlays)
            {
                if (!_overlayManager.HasOverlay(overlayContainer.ID) && TryCreateOverlayFromData(overlayContainer, out var overlay))
                {
                    _overlayManager.AddOverlay(overlayContainer.ID, overlay);
                }
            }

            SendNetworkMessage(new RequestOverlayEffectsSyncMessage(), _netManager.ServerChannel);
        }

        /// <summary>
        ///     Removes all overlays in <see cref="ActiveOverlays"/> from <see cref="IOverlayManager"/> that are active.
        /// </summary>
        private void DetachOverlaysFromManager()
        {
            ActiveOverlays.ForEach(o => _overlayManager.RemoveOverlay(o.ID));
        }






        /// <summary>
        ///     Syncs the current list of active overlays to be equivalent to the given list.
        /// </summary>
        private void SetOverlays(List<OverlayContainer> newOverlays)
        {
            var activeOverlayCopy = new List<OverlayContainer>(ActiveOverlays);
            foreach (var container in activeOverlayCopy)
            {
                var existingContainer = newOverlays.Find(c => c.ID == container.ID);
                if (existingContainer == null)
                    TryRemoveOverlay(container.ID);
            }
            foreach (var container in newOverlays)
            {
                if (!ActiveOverlays.Contains(container))
                    TryAddOverlayFromData(container);
                else
                    UpdateOverlayConfiguration(_overlayManager.GetOverlay(container.ID), container.Parameters);
            }
            ActiveOverlays = newOverlays;
        }

        /// <summary>
        ///     Updates a specific overlay with the given parameters.
        /// </summary>
        private void UpdateOverlay(Guid id, OverlayParameter[] parameters)
        {
            var overlay = _overlayManager.GetOverlay(id);
            if(overlay != null)
                UpdateOverlayConfiguration(overlay, parameters.ToList());
        }







        /// <summary>
        ///     Creates an instance of an overlay with the given data if no other overlay with the same ID exists, then adds it to <see cref="IOverlayManager"/>. Returns whether the operation was succesful.
        /// </summary>
        private bool TryAddOverlayFromData(OverlayContainer container)
        {
            if (_overlayManager.HasOverlay(container.ID))
                return false;

            if (TryCreateOverlayFromData(container, out var overlay))
            {
                _overlayManager.AddOverlay(container.ID, overlay);
                ActiveOverlays.Add(container);
                return true;
            }
            else
            {
                Logger.ErrorS("overlay", $"Could not add overlay {container.OverlayType}, as this OverlayType was not found!");
                return false;
            }
        }

        /// <summary>
        ///     Attempts to remove the overlay with the given ID. Returns whether the operation was successful.
        /// </summary>
        private bool TryRemoveOverlay(Guid id)
        {
            var container = ActiveOverlays.Find(c => c.ID == id);
            if (container != null)
            {
                ActiveOverlays.Remove(container);
                _overlayManager.RemoveOverlay(id);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Attempts to create a new overlay using the given data. Returns whether the operation was successful.
        /// </summary>
        private bool TryCreateOverlayFromData(OverlayContainer container, out Overlay overlay)
        {

            var stringClassName = container.OverlayType.ToString();
            var overlayTypes = _reflectionManager.GetAllChildren<Overlay>();
            var overlayType = overlayTypes.FirstOrDefault(t => t.Name == stringClassName);

            if (overlayType != null && overlayType.IsSubclassOf(typeof(Overlay)))
            {
                overlay = Activator.CreateInstance(overlayType) as Overlay;
                UpdateOverlayConfiguration(overlay, container.Parameters);
                return true;
            }

            overlay = default;
            return false;
        }

        /// <summary>
        ///     Runs all configure methods on the given overlay using the given list of parameters. If no such parameter exists, then the configuration function is not called.
        /// </summary>
        private void UpdateOverlayConfiguration(Overlay overlay, List<OverlayParameter> parameters)
        {
            var configurableTypes = overlay.GetType()
                .GetInterfaces()
                .Where(type =>
                    type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IConfigurable<>)
                    && parameters.Exists(p => p.GetType() == type.GenericTypeArguments.First()))
                .ToList();

            foreach (var type in configurableTypes)
            {
                var parameter = parameters.First(p => p.GetType() == type.GenericTypeArguments.First());
                if (parameter != null)
                {
                    var method = type.GetMethod(nameof(IConfigurable<object>.Configure));
                    method!.Invoke(overlay, new[] { parameter });
                }
            }
        }
    }
}
