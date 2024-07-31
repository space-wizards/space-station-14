using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Chat
{
    public sealed class SuicideSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        public bool Suicide(EntityUid victim, EntityUid mindId, MindComponent mind)
        {
            // Checks to see if the CannotSuicide tag exits, ghosts instead.
            if (_tagSystem.HasTag(victim, "CannotSuicide"))
                return false;

            // Checks to see if the player is dead.
            if (!TryComp<MobStateComponent>(victim, out var mobState) || _mobState.IsDead(victim, mobState))
                return false;

            _adminLogger.Add(LogType.Mind, $"{EntityManager.ToPrettyString(victim):player} is attempting to suicide");

            var suicideEvent = new SuicideEvent(victim);

            //Check to see if there were any systems blocking this suicide
            if (SuicideAttemptBlocked(victim, suicideEvent))
                return false;

            bool environmentSuicide = false;
            // If you are critical, you wouldn't be able to use your surroundings to suicide, so you do the default suicide
            if (!_mobState.IsCritical(victim, mobState))
            {
                environmentSuicide = EnvironmentSuicideHandler(victim, suicideEvent);
            }

            if (suicideEvent.AttemptBlocked)
                return false;

            if (!_gameTicker.OnGhostAttempt(mindId, false, mind: mind))
                return false;

            DefaultSuicideHandler(victim, suicideEvent);

            if (suicideEvent.Damage != null)
                ApplyLethalDamage(victim, suicideEvent.Damage);
            else
                ApplyLethalDamage(victim, suicideEvent.Kind);

            _adminLogger.Add(LogType.Mind, $"{EntityManager.ToPrettyString(victim):player} suicided{(environmentSuicide ? " (environment)" : "")}");
            return true;
        }

        /// <summary>
        /// If not handled, does the default suicide, which is biting your own tongue
        /// </summary>
        private void DefaultSuicideHandler(EntityUid victim, SuicideEvent suicideEvent)
        {
            if (suicideEvent.Handled)
                return;

            var othersMessage = Loc.GetString("suicide-command-default-text-others", ("name", victim));
            _popup.PopupEntity(othersMessage, victim, Filter.PvsExcept(victim), true);

            var selfMessage = Loc.GetString("suicide-command-default-text-self");
            _popup.PopupEntity(selfMessage, victim, victim);
            suicideEvent.Kind = "Bloodloss";
            suicideEvent.Handled = true;
        }

        /// <summary>
        /// Checks to see if there are any other systems that prevent suicide
        /// </summary>
        /// <returns>Returns true if there was a blocked attempt</returns>
        private bool SuicideAttemptBlocked(EntityUid victim, SuicideEvent suicideEvent)
        {
            RaiseLocalEvent(victim, suicideEvent, true);

            if (suicideEvent.AttemptBlocked)
                return true;

            return false;
        }

        /// <summary>
        /// Raise event to attempt to use held item, or surrounding entities to attempt to commit suicide
        /// </summary>
        private bool EnvironmentSuicideHandler(EntityUid victim, SuicideEvent suicideEvent)
        {
            var itemQuery = GetEntityQuery<ItemComponent>();

            // Suicide by held item
            if (EntityManager.TryGetComponent(victim, out HandsComponent? handsComponent)
                && handsComponent.ActiveHandEntity is { } item)
            {
                RaiseLocalEvent(item, suicideEvent, false);

                if (suicideEvent.Handled)
                    return true;
            }

            // Suicide by nearby entity (ex: Microwave)
            foreach (var entity in _entityLookupSystem.GetEntitiesInRange(victim, 1, LookupFlags.Approximate | LookupFlags.Static))
            {
                // Skip any nearby items that can be picked up, we already checked the active held item above
                if (itemQuery.HasComponent(entity))
                    continue;

                RaiseLocalEvent(entity, suicideEvent);

                if (suicideEvent.Handled)
                    return true;
            }

            return false;
        }

        private void ApplyLethalDamage(EntityUid target, DamageSpecifier? damage)
        {
            if (!TryComp<DamageableComponent>(target, out var damagable) || !TryComp<MobThresholdsComponent>(target, out var thresholds))
                return;

            var lethalAmountOfDamage = thresholds.Thresholds.Keys.Last() - damagable.TotalDamage;

            if (damage == null)
            {
                var damagePrototype = _prototypeManager.Index<DamageTypePrototype>("Blunt");
                damage = new DamageSpecifier(damagePrototype, lethalAmountOfDamage);
            }

            var finalDamage = new DamageSpecifier(damage);
            finalDamage.DamageDict.Remove("Structural");
            var totalItemDamage = finalDamage.GetTotal();
            foreach (var (key, value) in finalDamage.DamageDict)
            {
                finalDamage.DamageDict[key] = Math.Ceiling((double) (value * lethalAmountOfDamage / totalItemDamage));
            }
            _damageableSystem.TryChangeDamage(target, finalDamage, true, origin: target);
        }

        private void ApplyLethalDamage(EntityUid target, ProtoId<DamageTypePrototype>? kind)
        {
            if (!TryComp<DamageableComponent>(target, out var damagable) || !TryComp<MobThresholdsComponent>(target, out var thresholds))
                return;

            var lethalAmountOfDamage = thresholds.Thresholds.Keys.Last() - damagable.TotalDamage;

            if (!_prototypeManager.TryIndex<DamageTypePrototype>(kind, out var damagePrototype))
            {
                Log.Error($"{nameof(SuicideSystem)} could not find the damage type prototype associated with {kind}. Falling back to Blunt");
                damagePrototype = _prototypeManager.Index<DamageTypePrototype>("Blunt");
            }

            var damage = new DamageSpecifier(damagePrototype, lethalAmountOfDamage);

            var finalDamage = new DamageSpecifier(damage);
            finalDamage.DamageDict.Remove("Structural");
            var totalItemDamage = finalDamage.GetTotal();
            foreach (var (key, value) in finalDamage.DamageDict)
            {
                finalDamage.DamageDict[key] = Math.Ceiling((double) (value * lethalAmountOfDamage / totalItemDamage));
            }
            _damageableSystem.TryChangeDamage(target, finalDamage, true, origin: target);
        }
    }
}
