using Content.Server.Administration.Logs;
using Content.Server.Speech.Components;
using Content.Shared.Clothing;
using Content.Shared.Database;


namespace Content.Server.Speech.EntitySystems
{
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

            /* _cardSystem.*/
            TryChangeBattlecry(uid, args.Battlecry, meleeSpeechUser);
        }




        /// <summary>
        /// Attempts to change the battlecry of an entity.
        /// Returns true/false.
        /// </summary>
        /// <remarks>
        /// If provided with a player's EntityUid to the player parameter, adds the change to the admin logs.
        /// </remarks>
        public bool TryChangeBattlecry(EntityUid uid, string? battlecry, MeleeSpeechComponent? id = null, EntityUid? player = null)
        {
            if (!Resolve(uid, ref id))
                return false;

            if (!string.IsNullOrWhiteSpace(battlecry))
            {
                battlecry = battlecry.Trim();
            }
            else
            {
                battlecry = null;
            }

            if (id.Battlecry == battlecry)
                return true;
            id.Battlecry = battlecry;
            Dirty(id);

            if (player != null)
            {
                _adminLogger.Add(LogType.Identity, LogImpact.Low,  //I'm not exactly certain how admin logs should be categorized, let me know if this is wrong
                    $"{ToPrettyString(player.Value):player} has changed the battlecry of {ToPrettyString(id.Owner):entity} to {battlecry} ");
            }
            return true;
        }
    }
}
