using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Interfaces
{
    public interface IRadio
    {
        void Receiver(string message);

        void Broadcast(IRadio source, string message);

        int GetChannel();
    }
}
