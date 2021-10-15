using System.Threading.Tasks;
using Content.Server.Storage.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Plants.Components
{
    [RegisterComponent]
    public class PottedPlantHideComponent : Component, IInteractUsing, IInteractHand
    {
        public override string Name => "PottedPlantHide";

        [ViewVariables] private SecretStashComponent _secretStash = default!;
        [DataField("rustleSound")] private SoundSpecifier _rustleSound = new SoundPathSpecifier("/Audio/Effects/plant_rustle.ogg");

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
                Owner.PopupMessage(eventArgs.User, Loc.GetString("potted-plant-hide-component-interact-hand-got-no-item-message"));
            }

            return gotItem;
        }

        private void Rustle()
        {
            SoundSystem.Play(Filter.Pvs(Owner), _rustleSound.GetSound(), Owner, AudioHelpers.WithVariation(0.25f));
        }
    }
}
