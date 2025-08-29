using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Server.Chat.Systems;

namespace Content.Server.Clothing.Systems;

public sealed class SecurityHailerSystem : SharedSecurityHailerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HailerComponent, ActionSecHailerActionEvent>(OnHailOrder);
    }

    private void OnHailOrder(Entity<HailerComponent> ent, ref ActionSecHailerActionEvent ev)
    {
        if (ev.Handled)
            return;

        //Put the exclamations mark around people at the distance specified in the comp side
        //Just like a whistle
        bool exclamationHandled = base.ExclamateHumanoidsAround(ent);

        int index = base.PlayVoiceLineSound(ent); // index gotten from AudioSystem.ResolveSound() of the sound chosen from the soundcollection (Basically, which random line is playing ?)
        bool chatHandled = SayChatMessage(ent, ev, index);

        //If both exclamation and chat were done, we handled it yay !
        ev.Handled = exclamationHandled && chatHandled;
    }

    private bool SayChatMessage(Entity<HailerComponent> ent, ActionSecHailerActionEvent ev, int index)
    {
        string ftlLine = GetLineFormat(ent, index);

        //Make a chat line with the sec hailer as speaker, in bold and UPPERCASE for added impact
        _chat.TrySendInGameICMessage(ev.Performer, Loc.GetString(ftlLine).ToUpper(), InGameICChatType.Speak, hideChat: true, hideLog: true, nameOverride: ent.Comp.ChatName,
        checkRadioPrefix: false, ignoreActionBlocker: true, skipTransform: true);
        return true;
    }
}
