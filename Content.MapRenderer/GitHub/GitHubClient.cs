using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.Viewer;

namespace Content.MapRenderer.GitHub
{
    public class GitHubClient
    {
        private const string ApiEndpoint = "https://api.github.com/repos";

        private const string GitHubTokenEnvKey = "GITHUB_TOKEN";

        private readonly WebsiteViewer _viewer = new();
        private readonly string _repo;
        private readonly Lazy<HttpClient> _clientLazy = new(CreateClient);

        public GitHubClient(string repo)
        {
            _repo = repo;
        }

        private HttpClient Client => _clientLazy.Value;

        private string RepoUrl => $"{ApiEndpoint}/{_repo}";

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", EnvironmentExtensions.GetVariableOrThrow(GitHubTokenEnvKey));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Space Station 14 Map Diff");

            return client;
        }

        public string Write(IEnumerable<string> links)
        {
            var message = new StringBuilder();

            foreach (var link in links)
            {
                // TODO use old image link
                var viewerLink = _viewer.From(link, link);
                message.AppendLine(viewerLink);
            }

            return message.ToString();
        }

        public void Send(int issueNumber, string message)
        {
            Console.WriteLine($"Sending message in PR #{issueNumber}");

            message = HttpUtility.JavaScriptStringEncode(message);
            var endpoint = $"{RepoUrl}/issues/{issueNumber}/comments";

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("accept", "application/vnd.github.v3+json");
            request.Content = new StringContent(@$"{{""body"":""{message}""}}");

            HttpResponseMessage response = null;

            try
            {
                response = Client.Send(request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                Console.WriteLine($"Error sending GitHub message, error code {response?.StatusCode.ToString() ?? "None"}");
            }
        }
    }
}
