using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
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
                args.ToRemove &= ~MetaDataFlags.EntitySpecific;
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
            Dirty(component);
        }

        protected override bool PerformBasicActions(EntityUid user, ActionType action)
        {
            var result = base.PerformBasicActions(user, action);

            if (!string.IsNullOrWhiteSpace(action.Speech))
            {
                _chat.TrySendInGameICMessage(user, Loc.GetString(action.Speech), InGameICChatType.Speak, false);
                result = true;
            }

            return result;
        }
    }
}
