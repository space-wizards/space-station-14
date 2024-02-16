using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Notes;

[Serializable, NetSerializable]
public sealed class AdminMessageEuiState : EuiStateBase
{
    public float Time { get; set; }
    public string Message { get; set; }
    public string AdminName { get; set; }
    public DateTime AddedOn { get; set; }

    public AdminMessageEuiState(float time, string message, string adminName, DateTime addedOn)
    {
        Message = message;
        Time = time;
        AdminName = adminName;
        AddedOn = addedOn;
    }
}

public static class AdminMessageEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Accept : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class Dismiss : EuiMessageBase
    {
    }
}
