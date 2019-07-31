using System;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Stack
{
    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent]
    public class StackComponent : Component, IAttackBy, IExamine
    {
        private const string SerializationCache = "stack";
        private int _count = 50;
        private int _maxCount = 50;

        public override string Name => "Stack";

        [ViewVariables]
        public int Count
        {
            get => _count;
            private set
            {
                _count = value;
                if (_count <= 0)
                {
                    Owner.Delete();
                }
            }
        }

        [ViewVariables]
        public int MaxCount { get => _maxCount; private set => _maxCount = value; }

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
            }

            return false;
        }

        void IExamine.Examine(FormattedMessage message)
        {
            message.AddText($"There are {Count} things in the stack.");
        }
    }

    public enum StackType
    {
        Metal,
        Glass,
        Cable,
        Ointment,
        Brutepack,
    }
}
