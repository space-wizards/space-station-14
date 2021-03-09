#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking
{
    public abstract class SharedGameTicker
    {
        // See ideally these would be pulled from the job definition or something.
        // But this is easier, and at least it isn't hardcoded.
        public const string OverflowJob = "Assistant";
        public const string OverflowJobName = "assistant";

        protected class MsgTickerJoinLobby : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerJoinLobby);
            public MsgTickerJoinLobby(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }
        }

        protected class MsgTickerJoinGame : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerJoinGame);
            public MsgTickerJoinGame(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }
        }

        protected class MsgTickerLateJoinStatus : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLateJoinStatus);

            public bool Disallowed { get; set; }

            public MsgTickerLateJoinStatus(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                Disallowed = buffer.ReadBoolean();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(Disallowed);
            }
        }


        protected class MsgTickerLobbyStatus : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLobbyStatus);
            public MsgTickerLobbyStatus(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public bool IsRoundStarted { get; set; }
            public bool YouAreReady { get; set; }
            // UTC.
            public TimeSpan StartTime { get; set; }
            public bool Paused { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                IsRoundStarted = buffer.ReadBoolean();

                if (IsRoundStarted)
                {
                    return;
                }

                YouAreReady = buffer.ReadBoolean();
                StartTime = new TimeSpan(buffer.ReadInt64());
                Paused = buffer.ReadBoolean();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(IsRoundStarted);

                if (IsRoundStarted)
                {
                    return;
                }

                buffer.Write(YouAreReady);
                buffer.Write(StartTime.Ticks);
                buffer.Write(Paused);
            }
        }

        protected class MsgTickerLobbyInfo : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLobbyInfo);
            public MsgTickerLobbyInfo(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string TextBlob { get; set; } = string.Empty;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                TextBlob = buffer.ReadString();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(TextBlob);
            }
        }

        protected class MsgTickerLobbyCountdown : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLobbyCountdown);
            public MsgTickerLobbyCountdown(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            /// <summary>
            /// The game time that the game will start at.
            /// </summary>
            public TimeSpan StartTime { get; set; }

            /// <summary>
            /// Whether or not the countdown is paused
            /// </summary>
            public bool Paused { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                StartTime = new TimeSpan(buffer.ReadInt64());
                Paused = buffer.ReadBoolean();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(StartTime.Ticks);
                buffer.Write(Paused);
            }
        }

        protected class MsgTickerLobbyReady : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerLobbyReady);
            public MsgTickerLobbyReady(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            /// <summary>
            /// The Status of the Player in the lobby (ready, observer, ...)
            /// </summary>
            public Dictionary<NetUserId, PlayerStatus> PlayerStatus { get; set; } = new();

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                PlayerStatus = new Dictionary<NetUserId, PlayerStatus>();
                var length = buffer.ReadInt32();
                for (int i = 0; i < length; i++)
                {
                    var serializer = IoCManager.Resolve<IRobustSerializer>();
                    var byteLength = buffer.ReadVariableInt32();
                    NetUserId userId;
                    using (var stream = buffer.ReadAlignedMemory(byteLength))
                    {
                        serializer.DeserializeDirect(stream, out userId);
                    }
                    var status = (PlayerStatus)buffer.ReadByte();
                    PlayerStatus.Add(userId, status);
                }
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                buffer.Write(PlayerStatus.Count);
                foreach (var p in PlayerStatus)
                {
                    using (var stream = new MemoryStream())
                    {
                        serializer.SerializeDirect(stream, p.Key);
                        buffer.WriteVariableInt32((int) stream.Length);
                        stream.TryGetBuffer(out var segment);
                        buffer.Write(segment);
                    }
                    buffer.Write((byte)p.Value);
                }
            }
        }

        protected class MsgTickerJobsAvailable : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgTickerJobsAvailable);
            public MsgTickerJobsAvailable(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            /// <summary>
            /// The Status of the Player in the lobby (ready, observer, ...)
            /// </summary>
            public string[] JobsAvailable { get; set; } = Array.Empty<string>();

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                var amount = buffer.ReadInt32();
                JobsAvailable = new string[amount];

                for (var i = 0; i < amount; i++)
                {
                    JobsAvailable[i] = buffer.ReadString();
                }
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(JobsAvailable.Length);

                foreach (var job in JobsAvailable)
                {
                    buffer.Write(job);
                }
            }
        }

        public struct RoundEndPlayerInfo
        {
            public string PlayerOOCName;
            public string PlayerICName;
            public string Role;
            public bool Antag;
            public bool Observer;
        }

        protected class MsgRoundEndMessage : NetMessage
        {

            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgRoundEndMessage);
            public MsgRoundEndMessage(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public string GamemodeTitle = string.Empty;
            public string RoundEndText = string.Empty;
            public TimeSpan RoundDuration;


            public int PlayerCount;

            public List<RoundEndPlayerInfo> AllPlayersEndInfo = new();

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                GamemodeTitle = buffer.ReadString();
                RoundEndText = buffer.ReadString();

                var hours = buffer.ReadInt32();
                var mins = buffer.ReadInt32();
                var seconds = buffer.ReadInt32();
                RoundDuration = new TimeSpan(hours, mins, seconds);

                PlayerCount = buffer.ReadInt32();
                AllPlayersEndInfo = new List<RoundEndPlayerInfo>();
                for(var i = 0; i < PlayerCount; i++)
                {
                    var readPlayerData = new RoundEndPlayerInfo
                    {
                        PlayerOOCName = buffer.ReadString(),
                        PlayerICName = buffer.ReadString(),
                        Role = buffer.ReadString(),
                        Antag = buffer.ReadBoolean(),
                        Observer = buffer.ReadBoolean(),
                    };

                    AllPlayersEndInfo.Add(readPlayerData);
                }
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(GamemodeTitle);
                buffer.Write(RoundEndText);
                buffer.Write(RoundDuration.Hours);
                buffer.Write(RoundDuration.Minutes);
                buffer.Write(RoundDuration.Seconds);


                buffer.Write(AllPlayersEndInfo.Count);
                foreach(var playerEndInfo in AllPlayersEndInfo)
                {
                    buffer.Write(playerEndInfo.PlayerOOCName);
                    buffer.Write(playerEndInfo.PlayerICName);
                    buffer.Write(playerEndInfo.Role);
                    buffer.Write(playerEndInfo.Antag);
                    buffer.Write(playerEndInfo.Observer);
                }
            }

        }

        public enum PlayerStatus : byte
        {
            NotReady = 0,
            Ready,
            Observer,
        }
    }
}

