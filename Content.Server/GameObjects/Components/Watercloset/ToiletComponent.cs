#nullable enable
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Watercloset
{
    [RegisterComponent]
    public class ToiletComponent : Component, IInteractUsing, IMapInit
    {
        public sealed override string Name => "Toilet";

        private bool _isPrying = false;
        private float _pryLidTime = 1f;

        private bool _lidOpen = false;
        private bool _isSeatUp;

        [ViewVariables] public bool LidOpen => _lidOpen;
        [ViewVariables] public bool IsSeatUp => _isSeatUp;

        public void MapInit()
        {
            // roll is toilet seat will be up or down
            var random = IoCManager.Resolve<IRobustRandom>();
            _isSeatUp = random.NextBool();
            UpdateSprite();
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            // try pry open/close toilet lid
            if (!eventArgs.Using.TryGetComponent(out ToolComponent? tool))
                return false;

            // check if someone is already prying toilet
            if (_isPrying)
                return false;
            _isPrying = true;

            if (!await tool.UseTool(eventArgs.User, Owner, _pryLidTime, ToolQuality.Prying))
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
