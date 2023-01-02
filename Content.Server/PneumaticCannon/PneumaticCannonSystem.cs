using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.Interaction;
using Content.Shared.StatusEffect;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.PneumaticCannon
{
    public sealed class PneumaticCannonSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly GasTankSystem _gasTank = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly ContainerSystem _container = default!;
        [Dependency] private readonly PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PneumaticCannonComponent, InteractUsingEvent>(OnInteractUsing, before: new []{ typeof(StorageSystem) });
            SubscribeLocalEvent<PneumaticCannonComponent, AttemptShootEvent>(OnAttemptShoot);
        }

        private void OnInteractUsing(EntityUid uid, PneumaticCannonComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<ToolComponent>(args.Used, out var tool))
                return;

            if (!tool.Qualities.Contains(component.ToolModifyPower))
                return;

            var val = (int) component.Power;
            val = (val + 1) % (int) PneumaticCannonPower.Len;
            component.Power = (PneumaticCannonPower) val;

            _popup.PopupEntity(Loc.GetString("pneumatic-cannon-component-change-power",
                ("power", component.Power.ToString())), uid, args.User);

            // TODO Change gun stats

            args.Handled = true;
        }

        private void OnAttemptShoot(EntityUid uid, PneumaticCannonComponent component, ref AttemptShootEvent args)
        {
            if (!HasGas(uid, component, out var gas))
            {
                _popup.PopupEntity(Loc.GetString("pneumatic-cannon-component-fire-no-gas", ("cannon", uid)), uid, args.User);
                args.Cancelled = true;
                return;
            }

            if(EntityManager.TryGetComponent<StatusEffectsComponent?>(args.User, out var status)
               && component.Power == PneumaticCannonPower.High)
            {
                _stun.TryParalyze(args.User, TimeSpan.FromSeconds(component.HighPowerStunTime), true, status);
                _popup.PopupEntity(Loc.GetString("pneumatic-cannon-component-power-stun",
                    ("cannon", component.Owner)), uid, args.User);
            }

            var environment = _atmos.GetContainingMixture(component.Owner, false, true);
            var removed = _gasTank.RemoveAir(gas, GetMoleUsageFromPower(component.Power));
            if (environment != null && removed != null)
            {
                _atmos.Merge(environment, removed);
            }
        }

        /// <summary>
        ///     Returns whether the pneumatic cannon has enough gas to shoot an item.
        /// </summary>
        private bool HasGas(EntityUid uid, PneumaticCannonComponent component, [NotNullWhen(true)] out GasTankComponent? tank)
        {
            var usage = GetMoleUsageFromPower(component.Power);

            tank = null;
            if (!_container.TryGetContainer(uid, PneumaticCannonComponent.TankSlotId, out var container) ||
                container is not ContainerSlot slot || slot.ContainedEntity is not {} contained)
                return false;

            if (TryComp<GasTankComponent>(contained, out var gasTank))
            {
                if (gasTank.Air.TotalMoles < usage)
                    return false;

                tank = gasTank;
                return true;
            }

            return false;
        }

        private float GetRangeMultFromPower(PneumaticCannonPower power)
        {
            return power switch
            {
                PneumaticCannonPower.High => 1.6f,
                PneumaticCannonPower.Medium => 1.3f,
                PneumaticCannonPower.Low or _ => 1.0f,
            };
        }

        private float GetMoleUsageFromPower(PneumaticCannonPower power)
        {
            return power switch
            {
                PneumaticCannonPower.High => 9f,
                PneumaticCannonPower.Medium => 6f,
                PneumaticCannonPower.Low or _ => 3f,
            };
        }

        private float GetPushbackRatioFromPower(PneumaticCannonPower power)
        {
            return power switch
            {
                PneumaticCannonPower.Medium => 8.0f,
                PneumaticCannonPower.High => 16.0f,
                PneumaticCannonPower.Low or _ => 0f
            };
        }
    }
}
