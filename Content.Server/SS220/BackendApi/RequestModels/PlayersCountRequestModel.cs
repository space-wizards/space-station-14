namespace Content.Server.SS220.BackendApi.RequestModels;

public sealed class PlayersCountRequestModel : IBasicRequestModel
{
    public string WatchDogToken { get; set; } = string.Empty;
}
