using Content.Server.Plants.Components;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Plants.Systems
{
    public sealed class PottedPlantHideSystem : EntitySystem
    {
        [Dependency] private readonly SecretStashSystem _stashSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

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

            Rustle(uid, component);
            args.Handled = _stashSystem.TryHideItem(uid, args.User, args.Used);
        }

        private void OnInteractHand(EntityUid uid, PottedPlantHideComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            Rustle(uid, component);

            var gotItem = _stashSystem.TryGetItem(uid, args.User);
            if (!gotItem)
            {
                var msg = Loc.GetString("potted-plant-hide-component-interact-hand-got-no-item-message");
                _popupSystem.PopupEntity(msg, uid, args.User);
            }

            args.Handled = gotItem;
        }

        private void Rustle(EntityUid uid, PottedPlantHideComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            SoundSystem.Play(component.RustleSound.GetSound(), Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.25f));
        }
    }
}
