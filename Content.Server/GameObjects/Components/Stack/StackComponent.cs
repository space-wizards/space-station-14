using System;
using System.Threading.Tasks;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Stack
{

    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent]
    public class StackComponent : SharedStackComponent, IInteractUsing, IExamine
    {
        private bool _throwIndividually = false;

        public override int Count
        {
            get => base.Count;
            set => base.Count = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ThrowIndividually
        {
            get => _throwIndividually;
            private set
            {
                _throwIndividually = value;
                Dirty();
            }
        }

        public void Add(int amount)
        {
            Count += amount;
        }

        /// <summary>
        ///     Try to use an amount of items on this stack.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns>True if there were enough items to remove, false if not in which case nothing was changed.</returns>
        public bool Use(int amount)
        {
            if (Count >= amount)
            {
                Count -= amount;
                return true;
            }
            return false;
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (eventArgs.Using.TryGetComponent<StackComponent>(out var stack))
            {
                if (!stack.StackType.Equals(StackType))
                {
                    return false;
                }

                var toTransfer = Math.Min(Count, stack.AvailableSpace);
                Count -= toTransfer;
                stack.Add(toTransfer);

                var popupPos = eventArgs.ClickLocation;
                if (popupPos == EntityCoordinates.Invalid)
                {
                    popupPos = eventArgs.User.Transform.Coordinates;
                }


                if (toTransfer > 0)
                {
                    popupPos.PopupMessage(eventArgs.User, $"+{toTransfer}");

                    if (stack.AvailableSpace == 0)
                    {
                        Timer.Spawn(300, () => popupPos.PopupMessage(eventArgs.User, "Stack is now full."));
                    }

                    return true;
                }
                else if (toTransfer == 0 && stack.AvailableSpace == 0)
                {
                    popupPos.PopupMessage(eventArgs.User, "Stack is already full.");
                }
            }

            return false;
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange)
            {
                message.AddMarkup(Loc.GetPluralString(
                    "There is [color=lightgray]1[/color] thing in the stack",
                    "There are [color=lightgray]{0}[/color] things in the stack.", Count, Count));
            }
        }
    }
}
