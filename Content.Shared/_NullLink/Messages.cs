using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._NullLink;
public static class NullLink
{
    [Serializable, NetSerializable]
    public sealed class Subscribe() : EntityEventArgs
    {
    }
    [Serializable, NetSerializable]
    public sealed class Resubscribe() : EntityEventArgs
    {
    }
    [Serializable, NetSerializable]
    public sealed class Unsubscribe() : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class ServerData() : EntityEventArgs
    {
        public required Dictionary<string, Server> Servers { get; set; }
        public required Dictionary<string, ServerInfo> ServerInfo { get; set; }
    }

    [Serializable, NetSerializable]
    public abstract class UpdateEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class AddOrUpdateServer() : UpdateEvent
    {
        public required string Key { get; set; }
        public required Server Server { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class AddOrUpdateServerInfo() : UpdateEvent
    {
        public required string Key { get; set; }
        public required ServerInfo ServerInfo { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class RemoveServer() : UpdateEvent
    {
        public required string Key { get; set; }
    }

    [Serializable, NetSerializable]
    public record Server
    {
        public required string Title { get; set; }

        public string? Description { get; set; }

        public ServerType Type { get; set; }

        public bool IsAdultOnly { get; set; }

        public required string ConnectionString { get; set; }
    }

    [Serializable, NetSerializable]
    public enum ServerType : byte
    {
        NRP,
        LRP_minus,
        LRP,
        LRP_plus,
        MRP_minus,
        MRP,
        MRP_plus,
        HRP_minus,
        HRP,
        HRP_plus
    }

    [Serializable, NetSerializable]
    public record ServerInfo
    {
        public DateTime? СurrentStateStartedAt { get; set; }

        public int? MaxPlayers { get; set; }

        public int? Players { get; set; }

        public ServerStatus Status { get; set; }
        public string GetStatus()=> Status switch
        {
            ServerStatus.Offline => "Offline",
            ServerStatus.Lobby => "Lobby",
            ServerStatus.Round => "Round",
            ServerStatus.RoundEnding => "Round Ending",
            _ => "Unknown"
        };  
    }

    [Serializable, NetSerializable]
    public enum ServerStatus : byte
    {
        Offline,
        Lobby,
        Round,
        RoundEnding
    }
}
