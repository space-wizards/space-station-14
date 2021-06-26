#nullable enable
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Sandbox
{
    public abstract class SharedSandboxManager
    {
        protected sealed class MsgSandboxStatus : NetMessage
        {
            public override MsgGroups MsgGroup => MsgGroups.Command;

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
            public override MsgGroups MsgGroup => MsgGroups.Command;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }
        }

        protected sealed class MsgSandboxGiveAccess : NetMessage
        {
            public override MsgGroups MsgGroup => MsgGroups.Command;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }

        }

        protected sealed class MsgSandboxGiveAghost : NetMessage
        {
            public override MsgGroups MsgGroup => MsgGroups.Command;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }

        }

        protected sealed class MsgSandboxSuicide : NetMessage
        {
            public override MsgGroups MsgGroup => MsgGroups.Command;

            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
            }

        }
    }
}
