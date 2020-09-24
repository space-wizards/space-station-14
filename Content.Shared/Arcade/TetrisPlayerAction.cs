using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Arcade
{
    [Serializable, NetSerializable]
    public enum TetrisPlayerAction
    {
        NewGame, //todo
        StartGame, //todo
        Left,
        Right,
        Rotate,
        CounterRotate,
        SoftdropStart,
        SoftdropEnd,
        Harddrop,

        //todo Hold
    }
}
