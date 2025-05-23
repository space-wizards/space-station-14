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

        private void OnHailOrder(Entity<SecurityHailerComponent> ent, ref ActionSecHailerActionEvent ev)
        {
            //If the event is already handled
            if (ev.Handled)
                return;

            //Put the exclamations mark around people at the distance specified in the comp side
            //Just like a whistle
            bool exclamationHandled = base.ExclamateHumanoidsAround(ent);

            //Play the damn sound
            int index = base.PlayVoiceLine(ent); // index gotten from AudioSystem.ResolveSound() of the sound chosen in the soundcollection (Basically, which random line is playing ?)
            bool chatHandled = SayChatMessage(ent, ev, index);

            //If both exclamation and chat were done, we handled it yay !
            ev.Handled = exclamationHandled && chatHandled;
        }

        private bool SayChatMessage(Entity<SecurityHailerComponent> ent, ActionSecHailerActionEvent ev, int index)
        {
            string ftlLine = GetLineFormat(ent, index);
            var replacedLine = GetVoiceReplacement(ent, index);
            replacedLine ??= ftlLine;

            //Make a chat line with the sec hailer as speaker, in bold and UPPERCASE for added impact
            _chat.TrySendInGameICMessage(ev.Performer, Loc.GetString(replacedLine).ToUpper(), InGameICChatType.Speak, hideChat: false, hideLog: true, nameOverride: ent.Comp.ChatName,
            checkRadioPrefix: false, ignoreActionBlocker: true, skipTransform: true);
            return true;
        }
    }
}
