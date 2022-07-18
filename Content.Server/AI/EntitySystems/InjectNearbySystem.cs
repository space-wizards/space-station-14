using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.AI.Tracking;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Server.Silicons.Bots;
using Content.Shared.MobState.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server.AI.EntitySystems
{
    public sealed class InjectNearbySystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public EntityUid GetNearbyInjectable(EntityUid medibot, float range = 4)
        {
            foreach (var entity in _lookup.GetEntitiesInRange(medibot, range))
            {
                if (HasComp<InjectableSolutionComponent>(entity) && HasComp<MobStateComponent>(entity))
                    return entity;
            }

            return default;
        }

        public bool Inject(EntityUid medibot, EntityUid target)
        {
            if (!TryComp<MedibotComponent>(medibot, out var botComp))
                return false;

            if (!TryComp<DamageableComponent>(target, out var damage))
                return false;

            if (!_solutionSystem.TryGetInjectableSolution(target, out var injectable))
                return false;

            if (!_interactionSystem.InRangeUnobstructed(medibot, target))
                return true; // return true lets the bot reattempt the action on the same target

            if (damage.TotalDamage == 0)
                return false;

            if (damage.TotalDamage <= MedibotComponent.StandardMedDamageThreshold)
            {
                _solutionSystem.TryAddReagent(target, injectable, botComp.StandardMed, botComp.StandardMedInjectAmount, out var accepted);
                EnsureComp<RecentlyInjectedComponent>(target);
                _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, Filter.Entities(target));
                SoundSystem.Play("/Audio/Items/hypospray.ogg", Filter.Pvs(target), target);
                _chat.TrySendInGameICMessage(medibot, Loc.GetString("medibot-finish-inject"), InGameICChatType.Speak, false);
                return true;
            }

            if (damage.TotalDamage >= MedibotComponent.EmergencyMedDamageThreshold)
            {
                _solutionSystem.TryAddReagent(target, injectable, botComp.EmergencyMed, botComp.EmergencyMedInjectAmount, out var accepted);
                EnsureComp<RecentlyInjectedComponent>(target);
                _popupSystem.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, Filter.Entities(target));
                SoundSystem.Play("/Audio/Items/hypospray.ogg", Filter.Pvs(target), target);
                _chat.TrySendInGameICMessage(medibot, Loc.GetString("medibot-finish-inject"), InGameICChatType.Speak, false);
                return true;
            }

            return false;
        }
    }
}
