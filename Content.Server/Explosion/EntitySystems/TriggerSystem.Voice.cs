using Content.Server.Explosion.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Microsoft.CodeAnalysis.Options;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        private void InitializeVoice()
        {
            SubscribeLocalEvent<TriggerOnVoiceComponent, ExaminedEvent>(OnVoiceExamine);
            SubscribeLocalEvent<TriggerOnVoiceComponent, GetVerbsEvent<AlternativeVerb>>(OnVoiceGetAltVerbs);
        }

        private void OnVoiceGetAltVerbs(EntityUid uid, TriggerOnVoiceComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-trigger-voice-record"),
                Act = () => ToggleRecord(component, args.User),
                Priority = 1
            });
        }

        public void ToggleRecord(TriggerOnVoiceComponent component, EntityUid user, bool recorded = false)
        {
            component.IsRecording ^= true;

            if (recorded) //recording success popup
            {
                _popupSystem.PopupEntity(Loc.GetString("popup-trigger-voice-recorded"), component.Owner, Filter.Entities(user));
            }
            else if (component.IsRecording) //recording start popup
            {
                component.Activator = user;
                _popupSystem.PopupEntity(Loc.GetString("popup-trigger-voice-start-recording"), component.Owner, Filter.Entities(user));
            }
            else //recording stopped manually popup
            {
                _popupSystem.PopupEntity(Loc.GetString("popup-trigger-voice-stop-recording"), component.Owner, Filter.Entities(user));
            }
        }

        private void OnVoiceExamine(EntityUid uid, TriggerOnVoiceComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
                args.PushText(Loc.GetString("examine-trigger-voice", ("keyphrase", component.KeyPhrase?? Loc.GetString("trigger-voice-uninitialized"))));
        }
    }
}
