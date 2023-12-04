using Content.Shared.Audio;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Toilet
{
    public abstract class SharedToiletSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ToiletComponent, ComponentStartup>(OnComponentStartup);
            SubscribeLocalEvent<ToiletComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleSeatVerb);
            SubscribeLocalEvent<ToiletComponent, ActivateInWorldEvent>(OnActivateInWorld);
        }
        private void OnComponentStartup(EntityUid uid, ToiletComponent component, ComponentStartup args)
        {
            UpdateAppearance(uid, component);
        }

        private void OnToggleSeatVerb(EntityUid uid, ToiletComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || !CanToggle(uid))
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
            // we don't fuck with appearance data, and instead just manually update the sprite on the client
        }
    }
}
