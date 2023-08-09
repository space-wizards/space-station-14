using Content.Server.Administration.Logs;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Database;


namespace Content.Server.Speech.EntitySystems;

public sealed class MeleeSpeechSystem : SharedMeleeSpeechSystem
{

	[Dependency] private readonly IAdminLogManager _adminLogger = default!;


	public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechBattlecryChangedMessage>(OnBattlecryChanged);
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

