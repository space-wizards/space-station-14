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
    {
        /* Net stuff */
        public string Username = String.Empty;
        public NetUserId SessionId = new NetUserId();
        public short Ping = 0;
        public EntityUid EntityUid = EntityUid.Invalid;
        public SessionStatus Connected = SessionStatus.Zombie;
        public PlayerDisconnect? Disconnected = default;
        public bool Afk = false;

        /* IC stuff */
        public string CharacterName = String.Empty;
        public bool Antag = false;
        public string[] Roles = Array.Empty<string>();
        public bool DeadIC = false;
        public bool DeadPhysically = false;
        public TimeSpan? TimeOfDeath = default!;
        public MobStateFlags MobState = MobStateFlags.Unknown;
    }

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
