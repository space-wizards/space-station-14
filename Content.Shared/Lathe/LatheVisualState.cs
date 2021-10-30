using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Lathe
{
    [Serializable, NetSerializable]
    public enum LatheVisualState
    {
        Idle,
        Producing,
        InsertingMetal,
        InsertingGlass,
        InsertingGold,
        InsertingPlasma,
        InsertingPlastic
    }
}
