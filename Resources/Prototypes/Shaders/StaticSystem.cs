using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays
{
    /// <summary>
    /// This system manages the HUD overlays based on the player's equipped items, like showing the camera static shader
    /// when the "StaticViewer" component is equipped.
    /// </summary>
    public class StaticViewerHudSystem : EquipmentHudSystem<StaticViewerComponent>
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly ShaderInstance _staticShader;

        protected override SlotFlags TargetSlots => SlotFlags.ALL; // Or specify the slots you want to check for

        public StaticViewerHudSystem()
        {
            IoCManager.InjectDependencies(this);
            _staticShader = _prototypeManager.Index<ShaderPrototype>("CameraStatic").Instance().Duplicate();
        }

        protected override void UpdateInternal(RefreshEquipmentHudEvent<StaticViewerComponent> args)
        {
            base.UpdateInternal(args);
            
            // Check if the component is equipped, and apply the overlay
            if (args.Components.Count > 0)
            {
                ApplyStaticOverlay();
            }
            else
            {
                RemoveStaticOverlay();
            }
        }

        protected override void DeactivateInternal()
        {
            base.DeactivateInternal();

            // Remove the overlay when deactivating
            RemoveStaticOverlay();
        }

        private void ApplyStaticOverlay()
        {
            // Apply the "CameraStatic" overlay to the player's screen
            if (!_overlayManager.HasOverlay("CameraStatic"))
            {
                _overlayManager.AddOverlay(new ShaderOverlay(_staticShader));
            }
        }

        private void RemoveStaticOverlay()
        {
            // Remove the "CameraStatic" overlay
            if (_overlayManager.HasOverlay("CameraStatic"))
            {
                _overlayManager.RemoveOverlay("CameraStatic");
            }
        }

        protected override void OnCompEquip(EntityUid uid, StaticViewerComponent component, GotEquippedEvent args)
        {
            base.OnCompEquip(uid, component, args);
            
            // Apply the overlay when the item is equipped
            ApplyStaticOverlay();
        }

        protected override void OnCompUnequip(EntityUid uid, StaticViewerComponent component, GotUnequippedEvent args)
        {
            base.OnCompUnequip(uid, component, args);
            
            // Remove the overlay when the item is unequipped
            RemoveStaticOverlay();
        }

        // If you need to refresh the equipment and manage overlays
        protected override void OnRefreshComponentHud(EntityUid uid, StaticViewerComponent component, RefreshEquipmentHudEvent<StaticViewerComponent> args)
        {
            base.OnRefreshComponentHud(uid, component, args);

            // Apply the overlay when the equipment HUD is refreshed
            ApplyStaticOverlay();
        }
    }
}
