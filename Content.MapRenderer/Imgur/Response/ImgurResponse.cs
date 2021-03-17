using Newtonsoft.Json;

namespace Content.MapRenderer.Imgur.Response
{
    public class ImgurResponse
    {
        [JsonProperty("status")] public int Status { get; set; }

        [JsonProperty("success")] public bool Success { get; set; }

        [JsonProperty("data")] public ImgurUploadResponse Upload { get; set; }
    }
}
