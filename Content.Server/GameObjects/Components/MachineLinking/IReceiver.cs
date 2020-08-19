using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components.MachineLinking
{
    public interface IReceiver
    {
        void Trigger(bool state);
    }
}
