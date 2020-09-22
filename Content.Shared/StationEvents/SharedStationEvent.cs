using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Shared.StationEvents
{
    public class SharedStationEvent
    {
        public class MsgGetStationEvents : NetMessage
        {
            #region REQUIRED

            public const MsgGroups GROUP = MsgGroups.Command;
            public const string NAME = nameof(MsgGetStationEvents);
            public MsgGetStationEvents(INetChannel channel) : base(NAME, GROUP) { }

            #endregion
            public List<string> Events;
            public override void ReadFromBuffer(NetIncomingMessage buffer)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                var length = buffer.ReadVariableInt32();
                using var stream = buffer.ReadAsStream(length);
                Events = serializer.Deserialize<List<string>>(stream);
            }

            public override void WriteToBuffer(NetOutgoingMessage buffer)
            {
                var serializer = IoCManager.Resolve<IRobustSerializer>();
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, Events);
                    buffer.WriteVariableInt32((int)stream.Length);
                    stream.TryGetBuffer(out var segment);
                    buffer.Write(segment);
                }
            }
        }
    }
}
