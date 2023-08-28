namespace Content.Server.SS220.BackendApi.RequestModels;

public sealed class ConsoleCommandRequestModel : IBasicRequestModel
{
    public string Command { get; set; } = string.Empty;

    public string WatchDogToken { get; set; } = string.Empty;
}
