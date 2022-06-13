using Content.Shared.Vehicle.Components;
using Content.Shared.Vehicle;
using Content.Shared.Toggleable;
using Content.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server.Vehicle
{
    /// <summary>
    /// Controls all the vehicle horns.
    /// </summary>
    public sealed class HonkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VehicleComponent, HonkActionEvent>(OnHonk);
            SubscribeLocalEvent<VehicleComponent, ToggleActionEvent>(OnSirenToggle);
        }

        /// <summary>
        /// This fires when the rider presses the honk action
        /// </summary>
        private void OnHonk(EntityUid uid, VehicleComponent vehicle, HonkActionEvent args)
        {
            if (args.Handled)
                return;
            if (vehicle.HornSound != null)
            {
                SoundSystem.Play(vehicle.HornSound.GetSound(), Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.1f).WithVolume(8f));
                args.Handled = true;
            }
        }

        /// <summary>
        /// For vehicles with horn sirens (like the secway) this uses different logic that makes the siren
        /// loop instead of using a normal honk.
        /// </summary>
        private void OnSirenToggle(EntityUid uid, VehicleComponent vehicle, ToggleActionEvent args)
        {
            if (args.Handled || !vehicle.HornIsLooping)
                return;

            if (!vehicle.LoopingHornIsPlaying)
            {
                vehicle.SirenPlayingStream?.Stop();
                vehicle.LoopingHornIsPlaying = true;
                if (vehicle.HornSound != null)
                    vehicle.SirenPlayingStream = SoundSystem.Play(vehicle.HornSound.GetSound(), Filter.Pvs(uid), uid, AudioParams.Default.WithLoop(true).WithVolume(1.8f));
                return;
            }
            vehicle.SirenPlayingStream?.Stop();
            vehicle.LoopingHornIsPlaying = false;
        }
    }
}
