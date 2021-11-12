using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events;

[Serializable, NetSerializable]
public class RequestLogsMessage : EntityEventArgs
{
}
