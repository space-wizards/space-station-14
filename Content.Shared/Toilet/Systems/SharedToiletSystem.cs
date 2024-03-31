using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Plunger.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Toilet.Components;

namespace Content.Shared.Toilet.Systems
{
    /// <summary>
    /// Handles sprite changes for both toilet seat up and down as well as for lid open and closed. Handles interactions with hidden stash
    /// </summary>

    public abstract class SharedToiletSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ToiletComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ToiletComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleSeatVerb);
            SubscribeLocalEvent<ToiletComponent, ActivateInWorldEvent>(OnActivateInWorld);
        }

        private void OnMapInit(EntityUid uid, ToiletComponent component, MapInitEvent args)
        {
            if (_random.Prob(0.5f))
                component.ToggleSeat = true;

            if (_random.Prob(0.3f))
            {
                TryComp<PlungerUseComponent>(uid, out var plunger);

                if (plunger == null)
                    return;

                plunger.NeedsPlunger = true;
            }

            UpdateAppearance(uid);
            Dirty(uid, component);
        }

        public bool CanToggle(EntityUid uid)
        {
            return TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count == 0;
        }

        private void OnToggleSeatVerb(EntityUid uid, ToiletComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || !CanToggle(uid) || args.Hands == null)
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

            _audio.PlayPredicted(component.SeatSound, uid, uid);
            UpdateAppearance(uid, component);
            Dirty(uid, component, meta);
        }

        private void UpdateAppearance(EntityUid uid, ToiletComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _appearance.SetData(uid, ToiletVisuals.SeatVisualState, component.ToggleSeat ? SeatVisualState.SeatUp : SeatVisualState.SeatDown);
        }
    }
}
