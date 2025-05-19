using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Server.Chat.Systems;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Content.Shared.Emag.Components;

namespace Content.Server.Clothing.Systems
{
    public sealed class SecurityHailerSystem : SharedSecurityHailerSystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly AudioSystem _audio = default!;
        private readonly (string, string) _replaceLineHos = ("hail-high-8", "hail-high-HOS"); //the line to replace if a HOS gas mask hails

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
            string ftlLine = GetTheCorrectLine(ent, index);
            if (IsVoiceReplaced(ent, index)) //This is some bandaid code, replace it omg
                ftlLine = _replaceLineHos.Item2;

            //Make a chat line with the sec hailer as speaker, in bold and UPPERCASE for added impact
            _chat.TrySendInGameICMessage(ev.Performer, Loc.GetString(ftlLine).ToUpper(), InGameICChatType.Speak, hideChat: false, hideLog: true, nameOverride: ent.Comp.ChatName,
            checkRadioPrefix: false, ignoreActionBlocker: true, skipTransform: true);
            return true;
        }

        private int PlaySoundEffect(Entity<SecurityHailerComponent> ent)
        {
            var (uid, comp) = ent;

            SoundSpecifier currentSpecifier;
            if (comp.SpecialCircumtance == SecurityHailerComponent.SpecialUseCase.ERT)
                currentSpecifier = comp.ERTAggressionSounds;
            else if (HasComp<EmaggedComponent>(ent))
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

            if (IsVoiceReplaced(ent, collectionResolver.Index))
            {
                collectionResolver = (ResolvedCollectionSpecifier)_audio.ResolveSound(comp.HOSReplaceSounds); //add a check ? What to do if multiple in future ?
            }

            _audio.PlayPvs(resolver, ent.Owner, audioParams: new AudioParams().WithVolume(-3f));

            return collectionResolver.Index;
        }

        private bool IsVoiceReplaced(Entity<SecurityHailerComponent> ent, int index)
        {
            if (ent.Comp.SpecialCircumtance == SecurityHailerComponent.SpecialUseCase.HOS && GetTheCorrectLine(ent, index) == _replaceLineHos.Item1)
            {
                return true;
            }

            return false;
        }

        private string GetTheCorrectLine(Entity<SecurityHailerComponent> ent, int index)
        {
            string finalLine = String.Empty;
            if (HasComp<EmaggedComponent>(ent))
                finalLine = $"hail-emag-{index}";
            else if (ent.Comp.SpecialCircumtance == SecurityHailerComponent.SpecialUseCase.ERT)
                finalLine = $"hail-ERT-{index}";
            else
                finalLine = $"hail-{ent.Comp.AggresionLevel.ToString().ToLower()}-{index}";

            return finalLine;
        }
    }
}
