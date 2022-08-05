using Content.Server.Radio.EntitySystems;
using Content.Server.RadioKey.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Verbs;

namespace Content.Server.Headset
{
    public sealed class HeadsetSystem : EntitySystem
    {
        [Dependency] private readonly SharedRadioSystem _sharedRadioSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HeadsetComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<HeadsetComponent, GetVerbsEvent<AlternativeVerb>>(OnAltClick);
            SubscribeLocalEvent<HeadsetComponent, RadioChangeFrequency>(OnFrequencyChange);
        }

        private void OnFrequencyChange(EntityUid uid, HeadsetComponent component, RadioChangeFrequency args)
        {
            component.Frequency = _sharedRadioSystem.SanitizeFrequency(args.Frequency);
            _radioSystem.UpdateUIState(uid, component);
        }

        private void OnAltClick(EntityUid uid, HeadsetComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !component.Command)
            {
                return;
            }
            component.LoudMode = !component.LoudMode;
            component.Owner.PopupMessage(args.User, Loc.GetString("alt-click-headset", ("mode", component.LoudMode ? "on" : "off")));
        }

        private void OnExamined(EntityUid uid, HeadsetComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("examine-headset-far"));
                return;
            }
            args.PushMarkup(Loc.GetString("examine-headset"));
            // default prefix is first
            args.PushMarkup(Loc.GetString("examine-headset-chat-prefix", ("prefix", ";")));

            if (TryComp<RadioKeyComponent>(component.Owner, out var comp))
            {
                foreach (var freq in comp.UnlockedFrequency)
                {
                    var chan = _sharedRadioSystem.GetChannel(freq);
                    if (chan == null) continue; // this isnt supposed to happen

                    // even though this only appears on count 1 you can still use :u on multichannel, it just picks the first freq
                    if (comp.UnlockedFrequency.Count == 1)
                    {
                        args.PushMarkup(Loc.GetString("examine-headset-onechannel",
                            ("color", chan.Color),
                            ("key", chan.KeyCode),
                            ("id", chan.Name)));
                        return;
                    }

                    args.PushMarkup(Loc.GetString("examine-headset-channel",
                        ("color", chan.Color),
                        ("key", chan.KeyCode),
                        ("id", chan.Name)));
                }
            }
            if (component.Command) args.PushMarkup(Loc.GetString("examine-headset-loudmode"));

        }
    }
}
