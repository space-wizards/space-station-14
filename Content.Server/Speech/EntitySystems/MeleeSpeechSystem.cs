using Content.Server.Speech.Components;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Clothing;
//using Content.Shared.Access.Systems;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class MeleeSpeechSystem : SharedMeleeSpeechSystem
    {

        //[]


        public override void Initialize()
        {
            base.Initialize();
            //SubscribeLocalEvent<AgentIDCardComponent, AfterInteractEvent>(OnAfterInteract);
            //SubscribeLocalEvent<AgentIDCardComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<MeleeSpeechComponent, MeleeSpeechBattlecryChangedMessage>(OnBattlecryChanged);
        }

        /* private void OnMapInit(EntityUid uid, MeleeSpeechComponent meleeSpeech, MapInitEvent args)
         {
             UpdateEntityName(uid, meleeSpeech);
         }*/

        private void OnBattlecryChanged(EntityUid uid, MeleeSpeechComponent comp, MeleeSpeechBattlecryChangedMessage args)
        {
            if (!TryComp<MeleeSpeechComponent>(uid, out var meleeSpeechUser))
                return;

            /* _cardSystem.*/
            TryChangeBattlecry(uid, args.Battlecry, meleeSpeechUser);
        }




        /// <summary>
        /// Attempts to change the job title of a card.
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

                /*if (jobTitle.Length > IdCardConsoleComponent.MaxJobTitleLength)
                    jobTitle = jobTitle[..IdCardConsoleComponent.MaxJobTitleLength];*/
            }
            else
            {
                battlecry = null;
            }

            if (id.Battlecry == battlecry)
                return true;
            id.Battlecry = battlecry;
            Dirty(id);
            //UpdateEntityName(uid, id);

            if (player != null)
            {
              //  _adminLogger.Add(LogType.Identity, LogImpact.Low,
                //    $"{ToPrettyString(player.Value):player} has changed the battlecry of {ToPrettyString(id.Owner):entity} to {Battlecry} ");
            }
            return true;
        }
    }
}
