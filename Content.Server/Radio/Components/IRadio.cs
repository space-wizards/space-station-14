using System.Collections.Generic;

namespace Content.Server.Radio.Components
{
    public interface IRadio
    {
        IReadOnlyList<int> Channels { get; }

        void Receive(string message, int channel, EntityUidspeaker);

        void Broadcast(string message, EntityUidspeaker);
    }
}
