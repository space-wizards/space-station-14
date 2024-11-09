using Content.Server.PowerCell;
using Content.Shared.Alert;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.Jellids
{
    public sealed class JellidSystem : EntitySystem
    {
        [Dependency] private readonly PowerCellSystem _powerCell = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private const float DamageCycleInterval = 2.0f; // Time between damage cycles (in seconds)
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Subscribe to the events for handling power cell changes.
            SubscribeLocalEvent<JellidEntityComponent, PowerCellChangedEvent>(OnPowerCellChanged);
            SubscribeLocalEvent<JellidEntityComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
        }

        // Called when the power cell of the Jellid changes
        private void OnPowerCellChanged(EntityUid uid, JellidEntityComponent component, PowerCellChangedEvent args)
        {
            UpdateBatteryAlert(uid, component);

            // If the power cell is empty, start damage cycle
            if (!_powerCell.HasDrawCharge(uid))
            {
                StartDamageCycle(uid, component);
            }
            else
            {
                StopDamageCycle(uid, component);
            }
        }

        // Called when the power cell slot is empty
        private void OnPowerCellSlotEmpty(EntityUid uid, JellidEntityComponent component, ref PowerCellSlotEmptyEvent args)
        {
            // Start damage cycle when the power cell is empty
            StartDamageCycle(uid, component);
            UpdateBatteryAlert(uid, component);
        }

        // Starts a damage cycle when the power cell is empty
        private void StartDamageCycle(EntityUid uid, JellidEntityComponent component)
        {
            if (_mobState.IsAlive(uid))
            {
                // Start the damage cycle if it isn't already running
                if (!EntityManager.HasComponent<DamageCycleComponent>(uid))
                {
                    var cycleComponent = EntityManager.AddComponent<DamageCycleComponent>(uid);
                    cycleComponent.LastDamageTime = _gameTiming.RealTime;
                    cycleComponent.DamageCycleTimer = Timer.SpawnRepeating(DamageCycleInterval * 1000, () => DamageCycle(uid, component));
                }
            }
        }

        // Stops the damage cycle when the power cell is no longer empty
        private void StopDamageCycle(EntityUid uid, JellidEntityComponent component)
        {
            if (EntityManager.HasComponent<DamageCycleComponent>(uid))
            {
                var cycleComponent = EntityManager.GetComponent<DamageCycleComponent>(uid);
                // Cancel the timer and remove the cycle component
                cycleComponent.DamageCycleTimer.Cancel();
                EntityManager.RemoveComponent<DamageCycleComponent>(uid);
            }
        }

        // The damage cycle function that will apply damage at regular intervals
        private void DamageCycle(EntityUid uid, JellidEntityComponent component)
        {
            // Only apply damage if the power cell is still empty
            if (!_powerCell.HasDrawCharge(uid))
            {
                // Apply damage
                ApplyDamage(uid, component);
            }
            else
            {
                // Stop the damage cycle if power is restored
                StopDamageCycle(uid, component);
            }
        }

        // Applies damage to the Jellid when the power cell is empty
        private void ApplyDamage(EntityUid uid, JellidEntityComponent component)
        {
            // Placeholder damage application logic (adjust this as needed)
            var damageAmount = 10;  // Define how much damage the Jellid takes per cycle
            // For now, let's just log the damage
            // _log.Debug($"Jellid {uid} is taking {damageAmount} damage due to empty power cell.");

            // Example: Apply damage to health here (you can integrate with the actual damage system)
            // _damageable.TakeDamage(uid, damageAmount);

            // If using a health component, you would call something like:
            // _healthComponent.TakeDamage(damageAmount);

            // Optionally, add some visual/audio cues here when taking damage
        }

        // Updates the battery alert icon based on the power cell charge
        private void UpdateBatteryAlert(EntityUid uid, JellidEntityComponent component)
        {
            if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            {
                _alerts.ClearAlert(uid, component.BatteryAlert);
                _alerts.ShowAlert(uid, component.NoBatteryAlert);
                return;
            }

            var chargePercent = (short) MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

            // If the power is almost empty but not exactly zero, show the 1% alert
            if (chargePercent == 0 && _powerCell.HasDrawCharge(uid))
            {
                chargePercent = 1;
            }

            _alerts.ClearAlert(uid, component.NoBatteryAlert);
            _alerts.ShowAlert(uid, component.BatteryAlert, chargePercent);
        }
    }

    // Component to track the state of the damage cycle
    public class DamageCycleComponent : Component
    {
        public TimerHandle DamageCycleTimer;
        public DateTime LastDamageTime;
    }
}
