namespace Content.Server.Radio.Components.Telecomms;

public interface ITelecommsLogger : IComponent
{
    public List<TelecommsLog> TelecommsLog { get; set; }
}

public sealed class TelecommsLog
{
    [ViewVariables]
    public string UniqueName = "data packet (md5)";

    [ViewVariables]
    public string Speaker = default!;

    [ViewVariables]
    public string Job = "";

    [ViewVariables]
    public string Message = default!;

    [ViewVariables]
    public int Frequency = default!;
}
