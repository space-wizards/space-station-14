using Content.Shared.Disposal.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.MachineLinking
{
    [NetworkedComponent]
    public abstract class SharedSignalTimerComponent : Component
    {

    }

    [Serializable, NetSerializable]
    public enum SignalTimerUiKey
    {
        Key
    }
}
