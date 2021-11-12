using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[Serializable, NetSerializable]
public class LogsMessage : EntityEventArgs
{
    public LogsMessage(List<string> logs)
    {
        Logs = logs;
    }

    public List<string> Logs { get; set; }
}
