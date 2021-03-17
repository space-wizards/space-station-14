using Newtonsoft.Json;

namespace Content.MapRenderer.Imgur.Response
{
    public class ImgurRefreshTokenResponse
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }
    }
}
