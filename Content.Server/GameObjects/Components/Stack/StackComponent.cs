using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Stack
{

    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent]
    public class StackComponent : SharedStackComponent, IAttackBy, IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _sharedNotifyManager;
#pragma warning restore 649

        private const string SerializationCache = "stack";
        private int _count = 50;
        private int _maxCount = 50;

        [ViewVariables(VVAccess.ReadWrite)]
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                if (_count <= 0)
                {
                    Owner.Delete();
                }
                Dirty();
            }
        }

        [ViewVariables]
        public int MaxCount
        {
            get => _maxCount;
            private set
            {
                _maxCount = value;
                Dirty();
            }
        }

        [ViewVariables]
        public int AvailableSpace => MaxCount - Count;

        [ViewVariables]
        public object StackType { get; private set; }

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _maxCount, "max", 50);
            serializer.DataFieldCached(ref _count, "count", MaxCount);

            if (!serializer.Reading)
            {
                return;
            }

            if (serializer.TryGetCacheData(SerializationCache, out object stackType))
            {
                StackType = stackType;
                return;
            }

            if (serializer.TryReadDataFieldCached("stacktype", out string raw))
            {
                var refl = IoCManager.Resolve<IReflectionManager>();
                if (refl.TryParseEnumReference(raw, out var @enum))
                {
                    stackType = @enum;
                }
                else
                {
                    stackType = raw;
                }
            }
            else
            {
                stackType = Owner.Prototype.ID;
            }
            serializer.SetCacheData(SerializationCache, stackType);
            StackType = stackType;
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (eventArgs.AttackWith.TryGetComponent<StackComponent>(out var stack))
            {
                if (!stack.StackType.Equals(StackType))
                {
                    return false;
                }

                var toTransfer = Math.Min(Count, stack.AvailableSpace);
                Count -= toTransfer;
                stack.Add(toTransfer);

                var popupPos = eventArgs.ClickLocation;
                if (popupPos == GridCoordinates.InvalidGrid)
                {
                    popupPos = eventArgs.User.Transform.GridPosition;
                }


                if (toTransfer > 0)
                {
                    _sharedNotifyManager.PopupMessage(popupPos, eventArgs.User, $"+{toTransfer}");

                    if (stack.AvailableSpace == 0)
                    {

                        Timer.Spawn(300, () => _sharedNotifyManager.PopupMessage(popupPos, eventArgs.User, "Stack is now full."));
                    }

                }
                else if (toTransfer == 0 && stack.AvailableSpace == 0)
                {
                    _sharedNotifyManager.PopupMessage(popupPos, eventArgs.User, "Stack is already full.");
                }

            }

            return false;
        }

        void IExamine.Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();
            message.AddMarkup(loc.GetPluralString(
                "There is [color=lightgray]1[/color] thing in the stack",
                "There are [color=lightgray]{0}[/color] things in the stack.", Count, Count));
        }

        public override ComponentState GetComponentState()
        {
            return new StackComponentState(Count, MaxCount);
        }
    }

    public enum StackType
    {
        Metal,
        Glass,
        Cable,
        Ointment,
        Brutepack,
        FloorTileSteel
    }
}
