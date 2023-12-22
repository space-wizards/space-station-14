using Content.Server.Administration.Logs;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Speech.EntitySystems;

public sealed class MeleeSpeechSystem : SharedMeleeSpeechSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechBattlecryChangedMessage>(OnBattlecryChanged);
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechConfigureActionEvent>(OnConfigureAction);
        SubscribeLocalEvent<MeleeSpeechComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<MeleeSpeechComponent, MapInitEvent>(OnComponentMapInit);
    }
    private void OnComponentMapInit(EntityUid uid, MeleeSpeechComponent component, MapInitEvent args)
    {
        _actionSystem.AddAction(uid, ref component.ConfigureActionEntity, component.ConfigureAction, uid);
    }
    private void OnGetActions(EntityUid uid, MeleeSpeechComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ConfigureActionEntity, component.ConfigureAction);
    }
    private void OnBattlecryChanged(EntityUid uid, MeleeSpeechComponent comp, MeleeSpeechBattlecryChangedMessage args)
    {
        if (!TryComp<MeleeSpeechComponent>(uid, out var meleeSpeechUser))
            return;
        var battlecry = args.Battlecry;
        if (battlecry.Length > comp.MaxBattlecryLength)
            battlecry = battlecry[..comp.MaxBattlecryLength];
        TryChangeBattlecry(uid, battlecry, meleeSpeechUser);
    }
    /// <summary>
    /// Attempts to open the Battlecry UI.
    /// </summary>
    private void OnConfigureAction(EntityUid uid, MeleeSpeechComponent comp, MeleeSpeechConfigureActionEvent args)
    {
        TryOpenUi(args.Performer, uid, comp);
    }
    public void TryOpenUi(EntityUid user, EntityUid source, MeleeSpeechComponent? component = null)
    {
        if (!Resolve(source, ref component))
            return;
        if (!TryComp<ActorComponent>(user, out var actor))
            return;
        _uiSystem.TryToggleUi(source, MeleeSpeechUiKey.Key, actor.PlayerSession);
    }
    /// <summary>
    /// Attempts to change the battlecry of an entity.
    /// Returns true/false.
    /// </summary>
    /// <remarks>
    /// Logs changes to an entity's battlecry
    /// </remarks>
    public bool TryChangeBattlecry(EntityUid uid, string? battlecry, MeleeSpeechComponent? meleeSpeechComp = null)
    {
        if (!Resolve(uid, ref meleeSpeechComp))
            return false;
        if (!string.IsNullOrWhiteSpace(battlecry))
        {
            battlecry = battlecry.Trim();
        }
        else
        {
            battlecry = null;
        }
        if (meleeSpeechComp.Battlecry == battlecry)
            return true;
        meleeSpeechComp.Battlecry = battlecry;
        Dirty(uid, meleeSpeechComp);
        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $" {ToPrettyString(uid):entity}'s battlecry has been changed to {battlecry}");
        return true;
    }
}
