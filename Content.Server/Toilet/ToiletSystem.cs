using Content.Server.Body.Systems;
using Content.Shared.Buckle;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Toilet;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Server.Toilet
{
    public sealed class ToiletSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SecretStashSystem _secretStash = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedToolSystem _tool = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ToiletComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ToiletComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ToiletComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ToiletComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ToiletComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<ToiletComponent, SuicideEvent>(OnSuicide);
            SubscribeLocalEvent<ToiletComponent, ToiletPryDoAfterEvent>(OnToiletPried);
            SubscribeLocalEvent<ToiletComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleSeatVerb);
        }

        private void OnSuicide(EntityUid uid, ToiletComponent component, SuicideEvent args)
        {
            if (args.Handled)
                return;

            // Check that victim has a head
            // FIXME: since suiciding turns you into a ghost immediately, both messages are seen, not sure how this can be fixed
            if (TryComp<BodyComponent>(args.Victim, out var body) &&
                _body.BodyHasPartType(args.Victim, BodyPartType.Head, body))
            {
                var othersMessage = Loc.GetString("toilet-component-suicide-head-message-others",
                    ("victim", Identity.Entity(args.Victim, EntityManager)), ("owner", uid));
                _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(args.Victim), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString("toilet-component-suicide-head-message",
                    ("owner", uid));
                _popup.PopupEntity(selfMessage, uid, args.Victim, PopupType.LargeCaution);

                args.SetHandled(SuicideKind.Asphyxiation);
            }
            else
            {
                var othersMessage = Loc.GetString("toilet-component-suicide-message-others",
                    ("victim", Identity.Entity(args.Victim, EntityManager)), ("owner", uid));
                _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(uid), true, PopupType.MediumCaution);

                var selfMessage = Loc.GetString("toilet-component-suicide-message",
                    ("owner", uid));
                _popup.PopupEntity(selfMessage, uid, args.Victim, PopupType.LargeCaution);

                args.SetHandled(SuicideKind.Blunt);
            }
        }

        private void OnInit(EntityUid uid, ToiletComponent component, ComponentInit args)
        {
            EnsureComp<SecretStashComponent>(uid);
        }

        private void OnMapInit(EntityUid uid, ToiletComponent component, MapInitEvent args)
        {
            // roll is toilet seat will be up or down
            component.IsSeatUp = _random.Prob(0.5f);
            UpdateSprite(uid, component);
        }

        private void OnInteractUsing(EntityUid uid, ToiletComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // are player trying place or lift of cistern lid?
            if (_tool.UseTool(args.Used, args.User, uid, component.PryLidTime, component.PryingQuality, new ToiletPryDoAfterEvent()))
            {
                args.Handled = true;
            }
            // maybe player trying to hide something inside cistern?
            else if (component.LidOpen)
            {
                args.Handled = true;
                _secretStash.TryHideItem(uid, args.User, args.Used);
            }
        }

        private void OnInteractHand(EntityUid uid, ToiletComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            // trying get something from stash?
            if (component.LidOpen)
            {
                var gotItem = _secretStash.TryGetItem(uid, args.User);
                if (gotItem)
                {
                    args.Handled = true;
                    return;
                }
            }

            args.Handled = true;
        }

        private void OnToggleSeatVerb(EntityUid uid, ToiletComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || !CanToggle(uid))
                return;

            var alterToiletSeatText = component.IsSeatUp ? Loc.GetString("toilet-seat-close") : Loc.GetString("toilet-seat-open");

            var verb = new AlternativeVerb()
            {
                Act = () => {
                    if (CanToggle(uid))
                        ToggleToiletSeat(uid, component);
                },
                Text = alterToiletSeatText
            };

            args.Verbs.Add(verb);
        }

        private void OnExamine(EntityUid uid, ToiletComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange && component.LidOpen)
            {
                if (_secretStash.HasItemInside(uid))
                {
                    var msg = Loc.GetString("toilet-component-on-examine-found-hidden-item");
                    args.PushMarkup(msg);
                }
            }
        }

        private void OnToiletPried(EntityUid uid, ToiletComponent toilet, ToiletPryDoAfterEvent args)
        {
            if (args.Cancelled)
                return;

            toilet.LidOpen = !toilet.LidOpen;
            UpdateSprite(uid, toilet);
        }

        public bool CanToggle(EntityUid uid)
        {
            return TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count == 0;
        }

        public void ToggleToiletSeat(EntityUid uid, ToiletComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.IsSeatUp = !component.IsSeatUp;
            _audio.PlayPvs(component.ToggleSound, uid, AudioParams.Default.WithVariation(SharedContentAudioSystem.DefaultVariation));
            UpdateSprite(uid, component);
        }

        private void UpdateSprite(EntityUid uid, ToiletComponent component)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            _appearance.SetData(uid, ToiletVisuals.LidOpen, component.LidOpen, appearance);
            _appearance.SetData(uid, ToiletVisuals.SeatUp, component.IsSeatUp, appearance);
        }
    }
}
