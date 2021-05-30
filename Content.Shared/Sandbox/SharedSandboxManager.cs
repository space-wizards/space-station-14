#nullable enable
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Sandbox
{
    public abstract class SharedSandboxManager
    {
        protected sealed class MsgSandboxStatus : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgSandboxStatus);
            public MsgSandboxStatus(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public bool SandboxAllowed { get; set; }

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                SandboxAllowed = buffer.ReadBoolean();
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                buffer.Write(SandboxAllowed);
            }
        }

        protected sealed class MsgSandboxRespawn : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgSandboxRespawn);
            public MsgSandboxRespawn(INetChannel channel) : base(NAME, GROUP) { }

            #endregion

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }
        }

        protected sealed class MsgSandboxGiveAccess : NetMessage
        {
             #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgSandboxGiveAccess);
            public MsgSandboxGiveAccess(INetChannel channel) : base(NAME, GROUP) { }

            #endregion
            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }

        }

        protected sealed class MsgSandboxGiveAghost : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgSandboxGiveAghost);
            public MsgSandboxGiveAghost(INetChannel channel) : base(NAME, GROUP) { }

            #endregion
            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }

        }

        protected sealed class MsgSandboxSuicide : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgSandboxSuicide);
            public MsgSandboxSuicide(INetChannel channel) : base(NAME, GROUP) { }

            #endregion
            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }

        }
    }
}
