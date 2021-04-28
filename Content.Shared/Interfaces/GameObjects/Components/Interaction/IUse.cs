using System;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// This interface gives components behavior when using the entity in your active hand
    /// (done by clicking the entity in the active hand or pressing the keybind that defaults to Z).
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IUse
    {
        /// <summary>
        /// Called when we activate an object we are holding to use it
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use UseInHandMessage instead")]
        bool UseEntity(UseEntityEventArgs eventArgs);
    }

    public class UseEntityEventArgs : EventArgs
    {
        public UseEntityEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    ///     Raised when using the entity in your hands.
    /// </summary>
    [PublicAPI]
    public class UseInHandMessage : HandledEntityEventArgs
    {
        /// <summary>
        ///     Entity holding the item in their hand.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was used.
        /// </summary>
        public IEntity Used { get; }

        public UseInHandMessage(IEntity user, IEntity used)
        {
            User = user;
            Used = used;
        }
    }
}
