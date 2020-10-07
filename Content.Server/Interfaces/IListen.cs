using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Interfaces
{
    /// <summary>
    ///     Interface for objects such as radios meant to have an effect when speech is
    ///     heard. Requires component reference.
    /// </summary>
    public interface IListen : IComponent
    {
        int ListenRange { get; }

        bool CanListen(string message, IEntity source);

        void Listen(string message, IEntity speaker);
    }
}
