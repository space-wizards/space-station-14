using Content.Shared.Components;
using Content.Server.Popups;
using Robust.Shared.GameObjects;

namespace Content.Server.Systems
{
    public sealed class ListenerSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActiveListenerComponent, ListenEvent>(OnListen);
        }

        private void OnListen(EntityUid uid, ActiveListenerComponent component, ListenEvent args)
        {
            var message = args.Message.Trim();

            foreach (var codeword in component.Codewords)
            {
                if (message.Contains(codeword, StringComparison.InvariantCultureIgnoreCase))
                {
                    TriggerAction(uid, args.Source, codeword);
                    break;
                }
            }
        }

        private void TriggerAction(EntityUid uid, EntityUid source, string codeword)
        {
            // Action to unlock and open the uplink
            _popupSystem.PopupEntity($"The codeword '{codeword}' has been recognized.", uid);
            // Implement the uplink unlock and open logic here
        }
    }
}
