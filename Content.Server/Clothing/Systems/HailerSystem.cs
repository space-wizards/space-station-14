using Content.Server.Chat.Systems;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using JetBrains.FormatRipper.Elf;

namespace Content.Server.Clothing.Systems;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void SubmitChatMessage(Entity<HailerComponent> ent, string localeText, int index)
    {

        //Put the exclamations mark around people at the distance specified in the comp side
        //Just like a whistle
        //bool exclamationHandled = base.ExclamateHumanoidsAround(ent);

        //Make a chat line with the sec hailer as speaker, in bold and UPPERCASE for added impact
        string sentence = Loc.GetString(localeText + "-" + index);

        _chat.TrySendInGameICMessage(ent.Owner,
                                    sentence.ToUpper(),
                                    InGameICChatType.Speak,
                                    hideChat: true,
                                    hideLog: true,
                                    nameOverride: ent.Comp.ChatName,
                                    checkRadioPrefix: false,
                                    ignoreActionBlocker: true,
                                    skipTransform: true);
    }
}
