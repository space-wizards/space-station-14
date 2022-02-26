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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
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
