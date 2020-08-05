using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Interfaces
{
    public interface IRadio
    {
        void Receiver(string message);

        void Broadcast(string message);

        List<int> GetChannels();
    }
}
