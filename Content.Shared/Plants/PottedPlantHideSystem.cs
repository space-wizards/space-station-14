using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Plants
{
    public sealed class PottedPlantHideSystem : EntitySystem
    {
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SecretStashSystem _stashSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PottedPlantHideComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PottedPlantHideComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PottedPlantHideComponent, InteractHandEvent>(OnInteractHand);
        }

        private void OnInit(EntityUid uid, PottedPlantHideComponent component, ComponentInit args)
        {
            EntityManager.EnsureComponent<SecretStashComponent>(uid);
        }

        private void OnInteractUsing(EntityUid uid, PottedPlantHideComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            Rustle(uid, component, args.User);
            args.Handled = _stashSystem.TryHideItem(uid, args.User, args.Used);
        }

        private void OnInteractHand(EntityUid uid, PottedPlantHideComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            Rustle(uid, component, args.User);

            var gotItem = _stashSystem.TryGetItem(uid, args.User);
            if (!gotItem)
            {
                var msg = Loc.GetString("potted-plant-hide-component-interact-hand-got-no-item-message");
                _popupSystem.PopupClient(msg, uid, args.User);
            }

            args.Handled = gotItem;
        }

        private void Rustle(EntityUid uid, PottedPlantHideComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _audio.PlayPredicted(component.RustleSound, uid, user, AudioParams.Default.WithVariation(0.25f));
        }
    }
}
