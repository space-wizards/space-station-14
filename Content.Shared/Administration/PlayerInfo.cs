using System;
using Content.Shared.MobState.State;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    [Serializable, NetSerializable]
    public record PlayerInfo
    (
        /* Net stuff */
        string Username,
        NetUserId SessionId,
        short Ping,
        EntityUid EntityUid,
        SessionStatus Connected,
        PlayerDisconnect? Disconnected,
        bool Afk,

        /* IC stuff */
        string CharacterName,
        bool Antag,
        string[] Roles,
        bool DeadIC,
        bool DeadPhysically,
        TimeSpan? TimeOfDeath,
        MobStateFlags MobState
    );

    [Serializable, NetSerializable]
    public record PlayerDisconnect(DateTime stamp, string reason);

    // We make no assumptions about the underlying logic here, we just report what we're told.
    [Flags]
    [Serializable, NetSerializable]
    public enum MobStateFlags
    {
        Unknown       = 0b0000,
        Alive         = 0b0001,
        Critical      = 0b0010,
        Dead          = 0b0100,
        Incapacitated = 0b1000
    }

    public static class Extensions
    {
        public static MobStateFlags ToFlags(this IMobState state) =>
            (state.IsAlive() ? MobStateFlags.Alive : default)
            | (state.IsCritical() ? MobStateFlags.Critical : default)
            | (state.IsDead() ? MobStateFlags.Dead : default)
            | (state.IsIncapacitated() ? MobStateFlags.Incapacitated : default);
    }
}
