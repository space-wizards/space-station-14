using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Log;

namespace Content.Client.Overlays
{
    /// <summary>
    /// This system manages the HUD overlays based on the player's equipped items, like showing the camera static shader
    /// when the "StaticViewer" component is equipped.
    /// </summary>
    public class StaticViewerHudSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            Log.Error("StaticViewerHudSystem Initialized");

            // Subscribe to events for when the StaticViewerComponent is equipped and unequipped
            SubscribeLocalEvent<SkatesComponent, ClothingGotEquippedEvent>(OnGotEquipped);
            SubscribeLocalEvent<SkatesComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        }

        /// <summary>
        /// Called when the item with the StaticViewerComponent is equipped.
        /// </summary>
        public void OnGotEquipped(EntityUid uid, StaticViewerComponent component, OnGotEquippedEvent args)
        {
            Log.Error($"StaticViewerComponent equipped on entity {uid}, applying overlay.");
            ApplyStaticOverlay();
        }

        /// <summary>
        /// Called when the item with the StaticViewerComponent is unequipped.
        /// </summary>
        private void OnGotUnequipped(EntityUid uid, StaticViewerComponent component, OnGotUnequippedEvent args)
        {
            Log.Error($"StaticViewerComponent unequipped from entity {uid}, removing overlay.");
            RemoveStaticOverlay();
        }

        private void ApplyStaticOverlay()
        {
            // Add the static overlay to the player's screen
            Log.Error("Applying CameraStatic overlay");
            _overlayManager.AddOverlay(new ShaderOverlay("CameraStatic"));
        }

        private void RemoveStaticOverlay()
        {
            // Remove the static overlay from the player's screen
            Log.Error("Removing CameraStatic overlay");
            _overlayManager.RemoveOverlay("CameraStatic");
        }
    }
}
