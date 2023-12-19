using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Content.Shared.Tools.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Hands.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Examine;


namespace Content.Shared.Toilet
{
    /// <summary>
    /// Handles sprite changes for both troilet seat up and down as well as for lid open and closed. Handles interactions with hidden stash
    /// </summary>
    public abstract class SharedToiletSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SecretStashSystem _secretStash = default!;
        [Dependency] private readonly SharedToolSystem _tool = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ToiletComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<ToiletComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ToiletComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleSeatVerb);
            SubscribeLocalEvent<ToiletComponent, ActivateInWorldEvent>(OnActivateInWorld);
            SubscribeLocalEvent<ToiletComponent, ToiletPryDoAfterEvent>(OnToiletPried);
            SubscribeLocalEvent<ToiletComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ToiletComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ToiletComponent, ExaminedEvent>(OnExamine);
        }

        private void OnInit(EntityUid uid, ToiletComponent component, ComponentInit args)
        {
            EnsureComp<SecretStashComponent>(uid);
        }
        private void OnComponentStartup(EntityUid uid, ToiletComponent component, ComponentStartup args)
        {
            UpdateAppearance(uid, component);
        }

        private void OnInteractUsing(EntityUid uid, ToiletComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            // are player trying place or lift of cistern lid?
            else if (_tool.UseTool(args.Used, args.User, uid, component.PryLidTime, component.PryingQuality, new ToiletPryDoAfterEvent()))
            {
                args.Handled = true;
            }
            // maybe player trying to hide something inside cistern?
            else if (component.ToggleLid)
            {
                _secretStash.TryHideItem(uid, args.User, args.Used);
                args.Handled = true;
            }
        }

        private void OnInteractHand(EntityUid uid, ToiletComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            // trying get something from stash?
            if (component.ToggleLid)
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
            if (!args.CanInteract || !args.CanAccess || !CanToggle(uid) || !HasComp<HandsComponent>(args.User))
                return;

            AlternativeVerb toggleVerb = new()
            {
                Act = () => ToggleToiletSeat(uid, args.User, component)
            };
            if (component.ToggleSeat)
            {
                toggleVerb.Text = Loc.GetString("toilet-seat-close");
                toggleVerb.Icon =
                    new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
            }
            else
            {
                toggleVerb.Text = Loc.GetString("toilet-seat-open");
                toggleVerb.Icon =
                    new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
            }
            args.Verbs.Add(toggleVerb);
        }


        public bool CanToggle(EntityUid uid)
        {
            return TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count == 0;
        }

        private void OnActivateInWorld(EntityUid uid, ToiletComponent comp, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            ToggleToiletSeat(uid, args.User, comp);
        }

        public void ToggleToiletSeat(EntityUid uid, EntityUid? user = null, ToiletComponent? component = null, MetaDataComponent? meta = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.ToggleSeat = !component.ToggleSeat;
            Dirty(uid, component, meta);

            if (_timing.IsFirstTimePredicted)
            {
                UpdateAppearance(uid, component);
                _audio.PlayPredicted(component.Sound, uid, user, AudioParams.Default.WithVariation(0.15f));
            }
        }

        protected virtual void UpdateAppearance(EntityUid uid, ToiletComponent? component = null)
        {
        }

        private void OnToiletPried(EntityUid uid, ToiletComponent component, ToiletPryDoAfterEvent args)
        {
            if (args.Cancelled)
                return;

            ToggleLid(uid, component);
        }

        public void ToggleLid(EntityUid uid, ToiletComponent? component = null, MetaDataComponent? meta = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.ToggleLid = !component.ToggleLid;
            Dirty(uid, component, meta);

            if (_timing.IsFirstTimePredicted)
            {
                UpdateAppearance(uid, component);
            }
        }

        private void OnExamine(EntityUid uid, ToiletComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange && component.ToggleLid)
            {
                if (_secretStash.HasItemInside(uid))
                {
                    var msg = Loc.GetString("toilet-component-on-examine-found-hidden-item");
                    args.PushMarkup(msg);
                }
            }
        }
    }
}
