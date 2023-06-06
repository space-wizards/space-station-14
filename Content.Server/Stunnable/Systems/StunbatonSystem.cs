using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Events;
using Content.Server.Stunnable.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : EntitySystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
            SubscribeLocalEvent<StunbatonComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        }

        private void OnGetMeleeDamage(EntityUid uid, StunbatonComponent component, ref GetMeleeDamageEvent args)
        {
            if (!component.Activated)
                return;

            // Don't apply damage if it's activated; just do stamina damage.
            args.Damage = new DamageSpecifier();
        }

        private void OnStaminaHitAttempt(EntityUid uid, StunbatonComponent component, ref StaminaDamageOnHitAttemptEvent args)
        {
            if (!component.Activated ||
                !TryComp<BatteryComponent>(uid, out var battery) || !battery.TryUseCharge(component.EnergyPerUse))
            {
                args.Cancelled = true;
                return;
            }

            args.HitSoundOverride = component.StunSound;

            if (battery.CurrentCharge < component.EnergyPerUse)
            {
                SoundSystem.Play(component.SparksSound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), uid, AudioHelpers.WithVariation(0.25f));
                TurnOff(uid, component);
            }
        }

        private void OnUseInHand(EntityUid uid, StunbatonComponent comp, UseInHandEvent args)
        {
            if (comp.Activated)
            {
                TurnOff(uid, comp);
            }
            else
            {
                TurnOn(uid, comp, args.User);
            }
        }

        private void OnExamined(EntityUid uid, StunbatonComponent comp, ExaminedEvent args)
        {
            var msg = comp.Activated
                ? Loc.GetString("comp-stunbaton-examined-on")
                : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(msg);
            if(TryComp<BatteryComponent>(uid, out var battery))
                args.PushMarkup(Loc.GetString("stunbaton-component-on-examine-charge",
                    ("charge", (int)((battery.CurrentCharge/battery.MaxCharge) * 100))));
        }

        private void TurnOff(EntityUid uid, StunbatonComponent comp)
        {
            if (!comp.Activated)
                return;

            if (TryComp<AppearanceComponent>(comp.Owner, out var appearance) &&
                TryComp<ItemComponent>(comp.Owner, out var item))
            {
                _item.SetHeldPrefix(comp.Owner, "off", item);
                _appearance.SetData(uid, ToggleVisuals.Toggled, false, appearance);
            }

            SoundSystem.Play(comp.SparksSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner, AudioHelpers.WithVariation(0.25f));

            comp.Activated = false;
        }

        private void TurnOn(EntityUid uid, StunbatonComponent comp, EntityUid user)
        {
            if (comp.Activated)
                return;

            if (comp.IsRigged)
                Explode(uid, comp, user);

            var playerFilter = Filter.Pvs(comp.Owner, entityManager: EntityManager);
            if (!TryComp<BatteryComponent>(comp.Owner, out var battery) || battery.CurrentCharge < comp.EnergyPerUse)
            {
                SoundSystem.Play(comp.TurnOnFailSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("stunbaton-component-low-charge"));
                return;
            }

            if (EntityManager.TryGetComponent<AppearanceComponent>(comp.Owner, out var appearance) &&
                EntityManager.TryGetComponent<ItemComponent>(comp.Owner, out var item))
            {
                _item.SetHeldPrefix(comp.Owner, "on", item);
                _appearance.SetData(uid, ToggleVisuals.Toggled, true, appearance);
            }

            SoundSystem.Play(comp.SparksSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.25f));
            comp.Activated = true;
        }

        private void Explode(EntityUid uid, StunbatonComponent? comp = null, EntityUid? cause = null)
        {
            if (!Resolve(uid, ref comp))
                return;

            if (TryComp<BatteryComponent>(uid, out var battery))
            {
                if (_solutionsSystem.TryGetSolution(uid, StunbatonComponent.SolutionName, out var solution))
                {
                    solution.TryGetReagent("Plasma", out var plasma);
                    var radius = MathF.Min(plasma.Value, MathF.Sqrt(battery.CurrentCharge) / 9);
                    _explosionSystem.TriggerExplosive(uid, radius: radius, user:cause);
                }
            }
        }

        private void OnSolutionChange(EntityUid uid, StunbatonComponent comp, SolutionChangedEvent args)
        {
            comp.IsRigged = _solutionsSystem.TryGetSolution(uid, StunbatonComponent.SolutionName, out var solution)
                            && solution.TryGetReagent("Plasma", out var plasma)
                            && plasma >= 5;

            if (comp.IsRigged)
            {
                _adminLogger.Add(LogType.Explosion, LogImpact.Medium, $"Stunbaton {ToPrettyString(uid)} has been rigged up to explode when used.");
            }

            // You think you're funny, funny man?
            if (comp.Activated)
            {
                Explode(uid, comp);
            }
        }

        private void SendPowerPulse(EntityUid target, EntityUid? user, EntityUid used)
        {
            RaiseLocalEvent(target, new PowerPulseEvent()
            {
                Used = used,
                User = user
            }, false);
        }
    }
}
