using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Shared.Actions;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Database;
//using Content.Client.Weapons.Melee.UI;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Content.Server.Store.Components;
using Content.Shared.Store;
using Robust.Shared.Serialization;

namespace Content.Server.Speech.EntitySystems;

public sealed class MeleeSpeechSystem : SharedMeleeSpeechSystem
{

    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _playermanager = default!;
    public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechBattlecryChangedMessage>(OnBattlecryChanged);
        SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechConfigureActionEvent>(OnConfigureAction);
        SubscribeLocalEvent<MeleeSpeechComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnGetActions(EntityUid uid, MeleeSpeechComponent component, GetItemActionsEvent args)
    {
        if (component.ConfigureAction != null)
            args.Actions.Add(component.ConfigureAction);
    }

	private void OnBattlecryChanged(EntityUid uid, MeleeSpeechComponent comp, MeleeSpeechBattlecryChangedMessage args)
	{

        if (!TryComp<MeleeSpeechComponent>(uid, out var meleeSpeechUser))
            return;

        string battlecry = args.Battlecry;

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


    public void TryOpenUi(EntityUid user, EntityUid storeEnt, MeleeSpeechComponent? component = null)
    {
        if (!Resolve(storeEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (!_uiSystem.TryToggleUi(storeEnt, MeleeSpeechUiKey.Key, actor.PlayerSession))
            return;

        //UpdateUserInterface(user, storeEnt, component);
    }
    /* private bool TryOpenUi(EntityUid uid, MeleeSpeechComponent comp, MeleeSpeechConfigureActionEvent args)
     {

         if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor)) return false;

         return _uiSystem.TryToggleUi(uid, comp.MeleeSpeechUiKey, actor.PlayerSession);
     }
    */

    /// <summary>
    /// Attempts to change the battlecry of an entity.
    /// Returns true/false.
    /// </summary>
    /// <remarks>
    /// If provided with a player's EntityUid to the player parameter, adds the change to the admin logs.
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
		Dirty(meleeSpeechComp);

		_adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $" {ToPrettyString(uid):entity}'s battlecry has been changed to {battlecry}");
		return true;
	}
}

