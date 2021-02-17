using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.Audio;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class PottedPlantHideComponent : Component, IInteractUsing, IInteractHand
    {
        public override string Name => "PottedPlantHide";

        [ViewVariables] private SecretStashComponent _secretStash = default!;

        public override void Initialize()
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
            EntitySystem.Get<AudioSystem>()
                .PlayFromEntity("/Audio/Effects/plant_rustle.ogg", Owner, AudioHelpers.WithVariation(0.25f));
        }
    }
}
