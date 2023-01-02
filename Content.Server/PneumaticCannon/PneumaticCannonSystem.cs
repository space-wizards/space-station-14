using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Storage.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.PneumaticCannon
{
    public sealed class PneumaticCannonSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly GasTankSystem _gasTank = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PneumaticCannonComponent, InteractUsingEvent>(OnInteractUsing, before: new []{ typeof(StorageSystem) });
        }

        private void OnInteractUsing(EntityUid uid, PneumaticCannonComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!EntityManager.TryGetComponent<ToolComponent?>(args.Used, out var tool))
                return;
            if (!tool.Qualities.Contains(component.ToolModifyPower))
                return;

            var val = (int) component.Power;
            val = (val + 1) % (int) PneumaticCannonPower.Len;
            component.Power = (PneumaticCannonPower) val;

            args.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-change-power",
                ("power", component.Power.ToString())));

            args.Handled = true;
        }

        public void Fire(PneumaticCannonComponent comp, PneumaticCannonComponent.FireData data)
        {
            if (!HasGas(comp) && comp.GasTankRequired)
            {
                data.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-fire-no-gas",
                    ("cannon", comp.Owner)));
                SoundSystem.Play("/Audio/Items/hiss.ogg", Filter.Pvs(comp.Owner), comp.Owner, AudioParams.Default);
                return;
            }

            if(EntityManager.TryGetComponent<StatusEffectsComponent?>(data.User, out var status)
               && comp.Power == PneumaticCannonPower.High)
            {
                _stun.TryParalyze(data.User, TimeSpan.FromSeconds(comp.HighPowerStunTime), true, status);

                data.User.PopupMessage(Loc.GetString("pneumatic-cannon-component-power-stun",
                    ("cannon", comp.Owner)));
            }

            if (comp.GasTankSlot.ContainedEntity is {Valid: true} contained && comp.GasTankRequired)
            {
                // we checked for this earlier in HasGas so a GetComp is okay
                var gas = EntityManager.GetComponent<GasTankComponent>(contained);
                var environment = _atmos.GetContainingMixture(comp.Owner, false, true);
                var removed = _gasTank.RemoveAir(gas, GetMoleUsageFromPower(comp.Power));
                if (environment != null && removed != null)
                {
                    _atmos.Merge(environment, removed);
                }
            }
        }

        /// <summary>
        ///     Returns whether the pneumatic cannon has enough gas to shoot an item.
        /// </summary>
        public bool HasGas(PneumaticCannonComponent component)
        {
            var usage = GetMoleUsageFromPower(component.Power);

            if (component.GasTankSlot.ContainedEntity is not {Valid: true } contained)
                return false;

            // not sure how it wouldnt, but it might not! who knows
            if (EntityManager.TryGetComponent<GasTankComponent?>(contained, out var tank))
            {
                if (tank.Air.TotalMoles < usage)
                    return false;

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
