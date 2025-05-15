using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Server.Chat.Systems;

namespace Content.Server.Clothing.Systems
{
    public sealed class SecurityHailerSystem : SharedSecurityHailerSystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecurityHailerComponent, ActionSecHailerActionEvent>(OnHailOrder);
        }

        private void OnHailOrder(Entity<SecurityHailerComponent> ent, ref ActionSecHailerActionEvent args)
        {
            Log.Debug("OnHailOrder servers side reached !");

            //if (args.Handled)
            //    return;

            _chat.TrySendInGameICMessage(args.Performer, "HALT!!", InGameICChatType.Speak, true, true, checkRadioPrefix: false);  //Speech that isn't sent to chat or adminlogs
        }
    }
}
