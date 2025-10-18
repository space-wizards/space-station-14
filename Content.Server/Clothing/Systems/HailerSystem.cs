using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using JetBrains.FormatRipper.Elf;

namespace Content.Server.Clothing.Systems;

public sealed class HailerSystem : SharedHailerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void SubmitChatMessage(Entity<HailerComponent> ent, string localeText, int index)
    {

        //Put the exclamations mark around people at the distance specified in the comp side
        //Just like a whistle
        ExclamateHumanoidsAround(ent);

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

    protected override void IncreaseAggressionLevel(Entity<HailerComponent> ent)
    {
        if (ent.Comp.HailLevels != null)
        {
            //Up the aggression level or reset it
            ent.Comp.HailLevelIndex++;
            if (ent.Comp.HailLevelIndex >= ent.Comp.HailLevels.Count)
                ent.Comp.HailLevelIndex = 0;

            if (ent.Comp.CurrentHailLevel.HasValue)
                _popup.PopupEntity(Loc.GetString("hailer-gas-mask-screwed", ("level", ent.Comp.CurrentHailLevel.Value.Name.ToLower())), ent.Owner);
        }
    }


}
