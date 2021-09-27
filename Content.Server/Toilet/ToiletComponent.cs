using System.Threading.Tasks;
using Content.Server.Act;
using Content.Server.Buckle.Components;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Server.Tools.Components;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Toilet;
using Content.Shared.Tool;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Toilet
{
    [RegisterComponent]
    public class ToiletComponent : Component, IInteractUsing,
        IInteractHand, IMapInit, IExamine, ISuicideAct
    {
        public sealed override string Name => "Toilet";

        private const float PryLidTime = 1f;

        private bool _isPrying = false;

        [ViewVariables] public bool LidOpen { get; private set; }
        [ViewVariables] public bool IsSeatUp { get; private set; }

        [ViewVariables] private SecretStashComponent _secretStash = default!;

        [DataField("toggleSound")] SoundSpecifier _toggleSound = new SoundPathSpecifier("/Audio/Effects/toilet_seat_down.ogg");

        protected override void Initialize()
        {
            base.Initialize();
            _secretStash = Owner.EnsureComponent<SecretStashComponent>();
        }

        public void MapInit()
        {
            // roll is toilet seat will be up or down
            var random = IoCManager.Resolve<IRobustRandom>();
            IsSeatUp = random.Prob(0.5f);
            UpdateSprite();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // are player trying place or lift of cistern lid?
            if (eventArgs.Using.TryGetComponent(out ToolComponent? tool)
                && tool!.HasQuality(ToolQuality.Prying))
            {
                // check if someone is already prying this toilet
                if (_isPrying)
                    return false;
                _isPrying = true;

                if (!await tool.UseTool(eventArgs.User, Owner, PryLidTime, ToolQuality.Prying))
                {
                    _isPrying = false;
                    return false;
                }

                _isPrying = false;

                // all cool - toggle lid
                LidOpen = !LidOpen;
                UpdateSprite();

                return true;
            }
            // maybe player trying to hide something inside cistern?
            else if (LidOpen)
            {
                return _secretStash.TryHideItem(eventArgs.User, eventArgs.Using);
            }

            return false;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            // trying get something from stash?
            if (LidOpen)
            {
                var gotItem = _secretStash.TryGetItem(eventArgs.User);

                if (gotItem)
                    return true;
            }

            // just want to up/down seat?
            // check that nobody seats on seat right now
            if (Owner.TryGetComponent(out StrapComponent? strap))
            {
                if (strap.BuckledEntities.Count != 0)
                    return false;
            }

            ToggleToiletSeat();
            return true;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange && LidOpen)
            {
                if (_secretStash.HasItemInside())
                {
                    message.AddMarkup(Loc.GetString("toilet-component-on-examine-found-hidden-item"));
                }
            }
        }

        public void ToggleToiletSeat()
        {
            IsSeatUp = !IsSeatUp;
            SoundSystem.Play(Filter.Pvs(Owner), _toggleSound.GetSound(), Owner, AudioHelpers.WithVariation(0.05f));

            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(ToiletVisuals.LidOpen, LidOpen);
                appearance.SetData(ToiletVisuals.SeatUp, IsSeatUp);
            }
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            // check that victim even have head
            if (victim.TryGetComponent<SharedBodyComponent>(out var body) &&
                body.HasPartOfType(BodyPartType.Head))
            {
                var othersMessage = Loc.GetString("toilet-component-suicide-head-message-others", ("victim",victim.Name),("owner", Owner.Name));
                victim.PopupMessageOtherClients(othersMessage);

                var selfMessage = Loc.GetString("toilet-component-suicide-head-message", ("owner", Owner.Name));
                victim.PopupMessage(selfMessage);

                return SuicideKind.Asphyxiation;
            }
            else
            {
                var othersMessage = Loc.GetString("toilet-component-suicide-message-others",("victim", victim.Name),("owner", Owner.Name));
                victim.PopupMessageOtherClients(othersMessage);

                var selfMessage = Loc.GetString("toilet-component-suicide-message", ("owner",Owner.Name));
                victim.PopupMessage(selfMessage);

                return SuicideKind.Blunt;
            }
        }

    }
}
