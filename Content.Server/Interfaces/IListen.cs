using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Interfaces
{
    /// <summary>
    ///     Interface for objects such as radios meant to have an effect when speech is heard. Requires component reference.
    /// </summary>
    public interface IListen : IComponent
    {
        int ListenRange { get; }

        bool CanHear(string message, IEntity source);

        void Broadcast(string message, IEntity speaker);
    }
}
