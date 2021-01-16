#nullable enable
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Watercloset
{
    [RegisterComponent]
    public class ToiletComponent : Component, IInteractUsing, IInteractHand, IMapInit, IExamine
    {
        public sealed override string Name => "Toilet";

        private const float PryLidTime = 1f;

        private bool _isPrying = false;
        private bool _lidOpen = false;
        private bool _isSeatUp = false;

        [ViewVariables] public bool LidOpen => _lidOpen;
        [ViewVariables] public bool IsSeatUp => _isSeatUp;

        [ViewVariables] private SecretStashComponent _secretStash = default!;

        public override void Initialize()
        {
            base.Initialize();
            _secretStash = Owner.EnsureComponent<SecretStashComponent>();
        }

        public void MapInit()
        {
            // roll is toilet seat will be up or down
            var random = IoCManager.Resolve<IRobustRandom>();
            _isSeatUp = random.NextDouble() > 0.5f;
            UpdateSprite();
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
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
                _lidOpen = !_lidOpen;
                UpdateSprite();

                return true;
            }
            // maybe player trying to hide something inside cistern?
            else if (_lidOpen)
            {
                return _secretStash.TryHideItem(eventArgs.User, eventArgs.Using);
            }

            return false;
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
        {
            if (_lidOpen)
            {
                var gotItem = _secretStash.TryGetItem(eventArgs.User);
                return gotItem;
            }

            return false;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange && _lidOpen)
            {
                message.AddMarkup(Loc.GetString("The cistern lid seems to be open."));
                if (_secretStash.HasItemInside())
                {
                    message.AddMarkup(Loc.GetString("\nThere is [color=darkgreen]someting[/color] inside cistern!"));
                }
            }
        }

        public void ToggleToiletSeat()
        {
            _isSeatUp = !_isSeatUp;
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                var state = string.Format("{0}_toilet_{1}",
                    _lidOpen ? "open" : "closed",
                    _isSeatUp ? "seat_up" : "seat_down");

                sprite.LayerSetState(0, state);
            }
        }

        [Verb]
        public sealed class ToiletSeatVerb : Verb<ToiletComponent>
        {
            protected override void Activate(IEntity user, ToiletComponent component)
            {
                component.ToggleToiletSeat();
            }

            protected override void GetData(IEntity user, ToiletComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                var text = component.IsSeatUp ? "Put seat down" : "Put seat up";
                data.Text = Loc.GetString(text);
            }
        }
    }
}
