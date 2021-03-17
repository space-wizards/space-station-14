using Newtonsoft.Json;

namespace Content.MapRenderer.Imgur.Response
{
    public class ImgurUploadResponse
    {
        [JsonProperty("width")] public int Width { get; set; }

        [JsonProperty("height")] public int Height { get; set; }

        [JsonProperty("link")] public string Link { get; set; }
    }
}
