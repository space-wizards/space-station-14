using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Robust.Shared.Player;

namespace Content.Server.TapeRecorder
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class TapeRecorderSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TapeRecorderComponent, ChatMessageHeardNearbyEvent>(OnChatMessageHeard);
        }

        private void OnChatMessageHeard(EntityUid uid, TapeRecorderComponent component, ChatMessageHeardNearbyEvent args)
        {
            _chat.TrySendInGameICMessage(component.Owner, args.Message, InGameICChatType.Speak, args.HideChat);
            _popupSystem.PopupEntity(args.Message, component.Owner, Filter.Pvs(component.Owner));
        }
    }
}
