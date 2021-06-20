using System.Threading.Tasks;
using Content.Server.Storage.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Content.Server.Plants.Components
{
    [RegisterComponent]
    public class PottedPlantHideComponent : Component, IInteractUsing, IInteractHand
    {
        public override string Name => "PottedPlantHide";

        [ViewVariables] private SecretStashComponent _secretStash = default!;

        protected override void Initialize()
        {
            base.Initialize();
            _secretStash = Owner.EnsureComponent<SecretStashComponent>();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            Rustle();
            return _secretStash.TryHideItem(eventArgs.User, eventArgs.Using);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            Rustle();

            var gotItem = _secretStash.TryGetItem(eventArgs.User);
            if (!gotItem)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("You root around in the roots."));
            }

            return gotItem;
        }

        private void Rustle()
        {
            SoundSystem.Play(Filter.Pvs(Owner), "/Audio/Effects/plant_rustle.ogg", Owner, AudioHelpers.WithVariation(0.25f));
        }
    }
}
