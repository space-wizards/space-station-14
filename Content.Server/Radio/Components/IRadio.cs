using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.Server.Radio.Components
{
    public interface IRadio
    {
        IReadOnlyList<int> Channels { get; }

        void Receive(string message, int channel, IEntity speaker);

        void Broadcast(string message, IEntity speaker);
    }
}
