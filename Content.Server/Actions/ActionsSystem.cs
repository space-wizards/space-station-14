using Content.Server.Chat.Managers;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        [Dependency] private readonly IChatManager _chatMan = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ActionsComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<ActionsComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<ActionsComponent, MetaFlagRemoveAttemptEvent>(OnMetaFlagRemoval);
        }

        private void OnMetaFlagRemoval(EntityUid uid, ActionsComponent component, ref MetaFlagRemoveAttemptEvent args)
        {
            if (component.LifeStage == ComponentLifeStage.Running)
                args.Cancelled = true;
        }

        private void OnStartup(EntityUid uid, ActionsComponent component, ComponentStartup args)
        {
            _metaSystem.AddFlag(uid, MetaDataFlags.EntitySpecific);
        }

        private void OnShutdown(EntityUid uid, ActionsComponent component, ComponentShutdown args)
        {
            _metaSystem.RemoveFlag(uid, MetaDataFlags.EntitySpecific);
        }

        private void OnPlayerAttached(EntityUid uid, ActionsComponent component, PlayerAttachedEvent args)
        {
            // need to send state to new player.
            component.Dirty();
        }

        protected override bool PerformBasicActions(EntityUid user, ActionType action)
        {
            var result = base.PerformBasicActions(user, action);

            if (!string.IsNullOrWhiteSpace(action.Speech))
            {
                _chatMan.EntitySay(user, Loc.GetString(action.Speech));
                result = true;
            }

            return result;
        }
    }
}
