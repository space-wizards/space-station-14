using Content.Server.Act;
using Content.Server.Buckle.Components;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Toilet;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Toilet
{
    public sealed class ToiletSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SecretStashSystem _secretStash = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ToolSystem _toolSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ToiletComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ToiletComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ToiletComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ToiletComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ToiletComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<ToiletPryFinished>(OnToiletPried);
            SubscribeLocalEvent<ToiletPryInterrupted>(OnToiletInterrupt);
        }

        private void OnInit(EntityUid uid, ToiletComponent component, ComponentInit args)
        {
            EntityManager.EnsureComponent<SecretStashComponent>(uid);
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
            if (EntityManager.TryGetComponent(args.Used, out ToolComponent? tool)
                && tool.Qualities.Contains(component.PryingQuality))
            {
                // check if someone is already prying this toilet
                if (component.IsPrying)
                    return;
                component.IsPrying = true;

                // try to pry toilet cistern
                if (!_toolSystem.UseTool(args.Used, args.User, uid, 0f,
                    component.PryLidTime, component.PryingQuality,
                    new ToiletPryFinished(uid), new ToiletPryInterrupted(uid)))
                {
                    component.IsPrying = false;
                    return;
                }

                args.Handled = true;
            }
            // maybe player trying to hide something inside cistern?
            else if (component.LidOpen)
            {
                args.Handled = _secretStash.TryHideItem(uid, args.User, args.Used);
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

            // just want to up/down seat?
            // check that nobody seats on seat right now
            if (EntityManager.TryGetComponent(uid, out StrapComponent? strap))
            {
                if (strap.BuckledEntities.Count != 0)
                    return;
            }

            ToggleToiletSeat(uid, component);
            args.Handled = true;
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

        public SuicideKind Suicide(EntityUid uid, EntityUid victimUid, ToiletComponent? component = null,
            MetaDataComponent? meta = null, MetaDataComponent? victimMeta = null)
        {
            // check that victim even have head
            if (EntityManager.TryGetComponent<SharedBodyComponent>(victimUid, out var body) &&
                body.HasPartOfType(BodyPartType.Head))
            {
                var othersMessage = Loc.GetString("toilet-component-suicide-head-message-others",
                    ("victim", victimUid),("owner", uid));
                _popupSystem.PopupEntity(othersMessage, uid, Filter.Pvs(victimUid).RemoveWhereAttachedEntity(puid => puid == victimUid));

                var selfMessage = Loc.GetString("toilet-component-suicide-head-message",
                    ("owner", uid));
                _popupSystem.PopupEntity(selfMessage, uid, Filter.Entities(victimUid));

                return SuicideKind.Asphyxiation;
            }
            else
            {
                var othersMessage = Loc.GetString("toilet-component-suicide-message-others",
                    ("victim", victimUid),("owner", uid));
                _popupSystem.PopupEntity(othersMessage, uid, Filter.Pvs(uid).RemoveWhereAttachedEntity(puid => puid == victimUid));

                var selfMessage = Loc.GetString("toilet-component-suicide-message",
                    ("owner", uid));
                _popupSystem.PopupEntity(selfMessage, uid, Filter.Entities(victimUid));

                return SuicideKind.Blunt;
            }
        }

        private void OnToiletInterrupt(ToiletPryInterrupted ev)
        {
            if (!EntityManager.TryGetComponent(ev.Uid, out ToiletComponent? toilet))
                return;

            toilet.IsPrying = false;
        }

        private void OnToiletPried(ToiletPryFinished ev)
        {
            if (!EntityManager.TryGetComponent(ev.Uid, out ToiletComponent? toilet))
                return;

            toilet.IsPrying = false;
            toilet.LidOpen = !toilet.LidOpen;
            UpdateSprite(ev.Uid, toilet);
        }

        public void ToggleToiletSeat(EntityUid uid, ToiletComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.IsSeatUp = !component.IsSeatUp;
            SoundSystem.Play(Filter.Pvs(uid), component.ToggleSound.GetSound(), uid,
                AudioHelpers.WithVariation(0.05f));

            UpdateSprite(uid, component);
        }

        private void UpdateSprite(EntityUid uid, ToiletComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!EntityManager.TryGetComponent(uid,out AppearanceComponent? appearance))
                return;

            appearance.SetData(ToiletVisuals.LidOpen, component.LidOpen);
            appearance.SetData(ToiletVisuals.SeatUp, component.IsSeatUp);
        }
    }

    public sealed class ToiletPryFinished : EntityEventArgs
    {
        public EntityUid Uid;

        public ToiletPryFinished(EntityUid uid)
        {
            Uid = uid;
        }
    }

    public sealed class ToiletPryInterrupted : EntityEventArgs
    {
        public EntityUid Uid;

        public ToiletPryInterrupted(EntityUid uid)
        {
            Uid = uid;
        }
    }
}
