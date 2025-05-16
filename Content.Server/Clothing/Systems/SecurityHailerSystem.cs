using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Server.Chat.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server.Clothing.Systems
{
    public sealed class SecurityHailerSystem : SharedSecurityHailerSystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly AudioSystem _audio = default!;

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
            int index = PlaySoundEffect(ent); // index gotten from AudioSystem.ResolveSound() of the sound chosen in the soundcollection (Basically, which random line is playing ?)
            bool chatHandled = SayChatMessage(ent, ev, index);

            //If both exclamation and chat were done, we handled it yay !
            ev.Handled = exclamationHandled && chatHandled;
        }

        private bool SayChatMessage(Entity<SecurityHailerComponent> ent, ActionSecHailerActionEvent ev, int index)
        {
            //Make a chat line with the sec hailer as speaker, in bold and UPPERCASE for added impact
            string ftlLine = ent.Comp.Emagged ? $"hail-emag-{index}" : $"hail-{ent.Comp.AggresionLevel.ToString().ToLower()}-{index}"; //hail - aggression_level/emag - index
            _chat.TrySendInGameICMessage(ev.Performer, Loc.GetString(ftlLine).ToUpper(), InGameICChatType.Speak, hideChat: false, hideLog: true, nameOverride: ent.Comp.ChatName,
                checkRadioPrefix: false, ignoreActionBlocker: true, skipTransform: true);
            return true;
        }

        private int PlaySoundEffect(Entity<SecurityHailerComponent> ent)
        {
            var (uid, comp) = ent;

            SoundSpecifier currentSpecifier;
            if (ent.Comp.Emagged)
                currentSpecifier = ent.Comp.EmagAggressionSounds;
            else
            {
                currentSpecifier = comp.AggresionLevel switch
                {
                    SecurityHailerComponent.AggresionState.Medium => comp.MediumAggressionSounds,
                    SecurityHailerComponent.AggresionState.High => comp.HighAggressionSounds,
                    _ => comp.LowAggressionSounds,
                };
            }
            var resolver = _audio.ResolveSound(currentSpecifier);
            if (resolver is not ResolvedCollectionSpecifier collectionResolver)
                return -1;

            _audio.PlayPvs(resolver, ent.Owner); //TODO: check if works

            return collectionResolver.Index;
        }
    }
}
