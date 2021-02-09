#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Stack
{

    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent]
    [ComponentReference(typeof(SharedStackComponent))]
    public class StackComponent : SharedStackComponent, IInteractUsing, IExamine
    {
        private bool _throwIndividually = false;

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

        /// <summary>
        ///     Attempts to split this stack in two.
        /// </summary>
        /// <param name="amount">amount the new stack will have</param>
        /// <param name="spawnPosition">the position the new stack will spawn at</param>
        /// <param name="stack">the new stack</param>
        /// <returns></returns>
        public bool Split(int amount, EntityCoordinates spawnPosition, [NotNullWhen(true)] out IEntity? stack)
        {
            if (Count >= amount)
            {
                Count -= amount;

                stack = Owner.EntityManager.SpawnEntity(Owner.Prototype?.ID, spawnPosition);

                if (stack.TryGetComponent(out StackComponent? stackComp))
                {
                    stackComp.Count = amount;
                }

                return true;
            }

            stack = null;
            return false;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent<StackComponent>(out var stack))
                return false;

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
                    eventArgs.Using.SpawnTimer(300, () => popupPos.PopupMessage(eventArgs.User, "Stack is now full."));
                }
            }
            else if (toTransfer == 0 && stack.AvailableSpace == 0)
            {
                popupPos.PopupMessage(eventArgs.User, "Stack is already full.");
            }

            return true;
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
