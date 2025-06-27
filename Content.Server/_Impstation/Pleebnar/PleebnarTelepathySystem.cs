using Content.Server.Chat.Managers;
using Content.Server.DoAfter;
using Content.Shared._Impstation.Pleebnar;
using Content.Shared._Impstation.Pleebnar.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Pleebnar;
/// <summary>
/// handles the behaviour of the telepathy
/// </summary>
public sealed class PleebnarTelepathySystem : SharedPleebnarTelepathySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
        //init function
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyEvent>(Telepathy);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyDoAfterEvent>(TelepathyDoAfterEvent);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarTelepathyVisionMessage>(OnChangeVision);
        SubscribeLocalEvent<PleebnarTelepathyActionComponent, PleebnarVisionEvent>(OpenUi);

    }
    //after target is selected this is run
    public void Telepathy(Entity<PleebnarTelepathyActionComponent> ent, ref PleebnarTelepathyEvent args)
    {
        if (!TryComp<MindContainerComponent>(args.Target, out var mind))return; // try to get the mind container, if it fails return
        if (!mind.HasMind)//check if there is an active mind
        {
            _popupSystem.PopupEntity(Loc.GetString("pleebnar-telepathy-nomind"), ent, args.Performer,PopupType.SmallCaution);
            return;
        }
        if (ent.Comp.PleebnarVison == null)//check if player has selected a vision
        {
            _popupSystem.PopupEntity(Loc.GetString("pleebnar-telepathy-novision"), ent, args.Performer,PopupType.SmallCaution);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("pleebnar-focus"),ent.Owner,PopupType.Small);
        var doargs = new DoAfterArgs(EntityManager, ent, 1, new SharedPleebnarTelepathySystem.PleebnarTelepathyDoAfterEvent(), ent, args.Target)
        {
            DistanceThreshold = 5f,
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnMove = false,
            BreakOnWeightlessMove = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };
        _doAfter.TryStartDoAfter(doargs);
        args.Handled = true;


    }
    //send vision after a delay
    private void TelepathyDoAfterEvent(Entity<PleebnarTelepathyActionComponent> ent,
        ref PleebnarTelepathyDoAfterEvent args)
    {
        if (args.Target == null)//check if target still exists
        {
            return;
        }
        Filter visionAware = Filter.Empty().FromEntities([ent.Owner,(EntityUid)args.Target!]);// filter for chat message, contains the sender and receiver
        _chatManager.ChatMessageToManyFiltered(
            visionAware,
            ChatChannel.Notifications,
            Loc.GetString("pleebnar-telepathy-struck")+"\n"+Loc.GetString(ent.Comp.PleebnarVison!),
            Loc.GetString("pleebnar-telepathy-struck")+"\n"+Loc.GetString(ent.Comp.PleebnarVison!),
            (EntityUid)args.Target!,
            false,
            true,
            Color.MediumPurple,
            ent.Comp.WeirdAudioPath,
            -2f);
    }
    //handles telling the game to open the ui
    private void OpenUi(Entity<PleebnarTelepathyActionComponent> ent,ref PleebnarVisionEvent args)
    {
        var pleebnar = args.Action.Comp.Container;

        if (!TryComp<PleebnarTelepathyActionComponent>(pleebnar, out var telepathyComp))
            return;

        if (!_uiSystem.HasUi(pleebnar.Value, PleebnarTelepathyUIKey.Key))
            return;

        _uiSystem.OpenUi(pleebnar.Value, PleebnarTelepathyUIKey.Key, args.Performer);
        UpdateUI((pleebnar.Value, telepathyComp));
    }
    //handles sending the game info to update the ui, sends back the selected id pretty much
    private void UpdateUI(Entity<PleebnarTelepathyActionComponent> entity)
    {
        if (_uiSystem.HasUi(entity, PleebnarTelepathyUIKey.Key))
            _uiSystem.SetUiState(entity.Owner, PleebnarTelepathyUIKey.Key, new PleebnarTelepathyBuiState(entity.Comp.PleebnarVisonID));
    }
    //handles setting the selected vision
    private void OnChangeVision(Entity<PleebnarTelepathyActionComponent> entity, ref PleebnarTelepathyVisionMessage msg)
    {
        if(msg.Vision==null)return;
        if (msg.Vision is { } id && !_proto.HasIndex<PleebnarVisionPrototype>(id))
            return;
        var visProto = _proto.Index<PleebnarVisionPrototype>(msg.Vision!);
        entity.Comp.PleebnarVison = visProto.VisionString;
        entity.Comp.PleebnarVisonName = visProto.Name;
        entity.Comp.PleebnarVisonID = visProto.ID;
        _popupSystem.PopupEntity(Loc.GetString("pleebnar-telepathy-select")+" "+Loc.GetString(entity.Comp.PleebnarVisonName),entity.Owner,PopupType.Small);
        UpdateUI(entity);
    }

}
