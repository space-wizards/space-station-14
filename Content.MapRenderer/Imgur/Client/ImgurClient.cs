using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.Imgur.Response;
using Newtonsoft.Json;
using SixLabors.ImageSharp;

namespace Content.MapRenderer.Imgur.Client
{
    public class ImgurClient
    {
        private const string RefreshTokenUrl = "https://api.imgur.com/oauth2/token";
        private const string UploadUrl = "https://api.imgur.com/3/upload";

        private const string RefreshTokenEnvKey = "IMGUR_REFRESH_TOKEN";
        private const string ClientIdEnvKey = "IMGUR_CLIENT_ID";
        private const string ClientSecretEnvKey = "IMGUR_CLIENT_SECRET";

        private readonly Lazy<HttpClient> _clientLazy = new(CreateClient().Result);

        private HttpClient Client => _clientLazy.Value;

        private static async Task<HttpClient> CreateClient()
        {
            var client = new HttpClient();

            var values = new Dictionary<string, string>
            {
                ["refresh_token"] = EnvironmentExtensions.GetVariableOrThrow(RefreshTokenEnvKey),
                ["client_id"] = EnvironmentExtensions.GetVariableOrThrow(ClientIdEnvKey),
                ["client_secret"] = EnvironmentExtensions.GetVariableOrThrow(ClientSecretEnvKey),
                ["grant_type"] = "refresh_token"
            };

            var content = new FormUrlEncodedContent(values!);
            var response = await client.PostAsync(RefreshTokenUrl, content);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<ImgurRefreshTokenResponse>(responseString);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", responseObject.AccessToken);

            return client;
        }

        public async Task<ImgurUploadResponse> Upload(Image image)
        {
            Console.WriteLine($"Uploading image with height {image.Height} and width {image.Width} to imgur");

            byte[] data;

            await using (var stream = new MemoryStream())
            {
                await image.SaveAsPngAsync(stream);

                data = stream.ToArray();
            }

            var values = new Dictionary<string, string>
            {
                ["image"] = Convert.ToBase64String(data),
                ["type"] = "base64",
                ["Authorization"] = $"Client-ID {EnvironmentExtensions.GetVariableOrThrow(ClientIdEnvKey)}"
            };

            var content = new FormUrlEncodedContent(values!);

            var response = await Client.PostAsync(UploadUrl, content);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ImgurUploadResponse>(responseString);
        }
    }
}
