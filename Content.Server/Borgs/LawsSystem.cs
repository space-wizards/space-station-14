using System.Linq;
using Content.Shared.Borgs;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using JetBrains.Annotations;

namespace Content.Server.Borgs
{
    public sealed class LawsSystem : SharedLawsSystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<LawsComponent, StateLawsMessage>(OnStateLaws);
            SubscribeLocalEvent<LawsComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        [PublicAPI]
        public bool TryStateLaws(EntityUid uid, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!component.CanState)
                return false;

            if (component.StateTime != null && _timing.CurTime < component.StateTime)
                return false;

            component.StateTime = _timing.CurTime + component.StateCD;

            foreach (var law in component.Laws)
            {
                _chat.TrySendInGameICMessage(uid, law.Value.Text, InGameICChatType.Speak, false);
            }

            return true;
        }
        private void OnStateLaws(EntityUid uid, LawsComponent component, StateLawsMessage args)
        {
            TryStateLaws(uid, component);
        }

        private void OnPlayerAttached(EntityUid uid, LawsComponent component, PlayerAttachedEvent args)
        {
            if (!_uiSystem.TryGetUi(uid, LawsUiKey.Key, out var ui))
                return;

            _uiSystem.TryOpen(uid, LawsUiKey.Key, args.Player);
        }
    }
}
