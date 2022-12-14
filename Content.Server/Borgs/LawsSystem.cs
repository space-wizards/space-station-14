using Content.Shared.Borgs;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using JetBrains.Annotations;

namespace Content.Server.Borgs
{
    public sealed class LawsSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LawsComponent, StateLawsMessage>(OnStateLaws);
            SubscribeLocalEvent<LawsComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnStateLaws(EntityUid uid, LawsComponent component, StateLawsMessage args)
        {
            StateLaws(uid, component);
        }

        private void OnPlayerAttached(EntityUid uid, LawsComponent component, PlayerAttachedEvent args)
        {
            if (!_uiSystem.TryGetUi(uid, LawsUiKey.Key, out var ui))
                return;

            _uiSystem.TryOpen(uid, LawsUiKey.Key, args.Player);
        }

        public void StateLaws(EntityUid uid, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!component.CanState)
                return;

            if (component.StateTime != null && _timing.CurTime < component.StateTime)
                return;

            component.StateTime = _timing.CurTime + component.StateCD;

            foreach (var law in component.Laws)
            {
                _chat.TrySendInGameICMessage(uid, law, InGameICChatType.Speak, false);
            }
        }

        [PublicAPI]
        public void ClearLaws(EntityUid uid, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            component.Laws.Clear();
            Dirty(component);
        }

        public void AddLaw(EntityUid uid, string law, int? index = null, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            if (index == null)
                index = component.Laws.Count;

            index = Math.Clamp((int) index, 0, component.Laws.Count);

            component.Laws.Insert((int) index, law);
            Dirty(component);
        }

        public void RemoveLaw(EntityUid uid, int? index = null, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            if (index == null)
                index = component.Laws.Count;

            if (component.Laws.Count == 0)
                return;

            index = Math.Clamp((int) index, 0, component.Laws.Count - 1);

            if (index < 0)
                return;

            component.Laws.RemoveAt((int) index);
            Dirty(component);
        }
    }
}
