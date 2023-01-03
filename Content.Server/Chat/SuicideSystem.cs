using Content.Server.Administration.Logs;
using Content.Server.Hands.Components;
using Content.Server.MobState;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

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

        public bool Suicide(EntityUid victim)
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

            DefaultSuicideHandler(victim, suicideEvent);

            ApplyDeath(victim, suicideEvent.Kind!.Value);
            _adminLogger.Add(LogType.Mind, $"{EntityManager.ToPrettyString(victim):player} suicided{(environmentSuicide ? " (environment)" : "")}");
            return true;
        }

        /// <summary>
        /// If not handled, does the default suicide, which is biting your own tongue
        /// </summary>
        private static void DefaultSuicideHandler(EntityUid victim, SuicideEvent suicideEvent)
        {
            if (suicideEvent.Handled)
                return;

            var othersMessage = Loc.GetString("suicide-command-default-text-others", ("name", victim));
            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("suicide-command-default-text-self");
            victim.PopupMessage(selfMessage);
            suicideEvent.SetHandled(SuicideKind.Bloodloss);
        }

        /// <summary>
        /// Checks to see if there are any other systems that prevent suicide
        /// </summary>
        /// <returns>Returns true if there was a blocked attempt</returns>
        private bool SuicideAttemptBlocked(EntityUid victim, SuicideEvent suicideEvent)
        {
            RaiseLocalEvent(victim, suicideEvent, false);

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

                RaiseLocalEvent(entity, suicideEvent, false);

                if (suicideEvent.Handled)
                    return true;
            }

            return false;
        }

        private void ApplyDeath(EntityUid target, SuicideKind kind)
        {
            if (kind == SuicideKind.Special)
                return;

            if (!_prototypeManager.TryIndex<DamageTypePrototype>(kind.ToString(), out var damagePrototype))
            {
                const SuicideKind fallback = SuicideKind.Blunt;
                Logger.Error($"{nameof(SuicideSystem)} could not find the damage type prototype associated with {kind}. Falling back to {fallback}");
                damagePrototype = _prototypeManager.Index<DamageTypePrototype>(fallback.ToString());
            }
            const int lethalAmountOfDamage = 200; // TODO: Would be nice to get this number from somewhere else
            _damageableSystem.TryChangeDamage(target, new(damagePrototype, lethalAmountOfDamage), true, origin: target);
        }
    }
}
