using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Content.MapRenderer.Extensions;
using Content.MapRenderer.Viewer;

namespace Content.MapRenderer.GitHub
{
    public class GitHubClient
    {
        private const string ApiEndpoint = "https://api.github.com/repos";

        private const string GitHubTokenEnvKey = "GITHUB_TOKEN";

        private readonly WebsiteViewer _viewer = new();
        private readonly string _owner;
        private readonly string _repo;
        private readonly Lazy<HttpClient> _clientLazy = new(CreateClient);

        public GitHubClient(string owner, string repo)
        {
            _owner = owner;
            _repo = repo;
        }

        private HttpClient Client => _clientLazy.Value;

        private string RepoUrl => $"{ApiEndpoint}/{_owner}/{_repo}";

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", EnvironmentExtensions.GetVariableOrThrow(GitHubTokenEnvKey));

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

        public async void Send(int issueNumber, string message)
        {
            var endpoint = $"{RepoUrl}/issues/{issueNumber}/comments";

            var values = new Dictionary<string, string>
            {
                ["accept"] = "application/vnd.github.v3+json",
                ["owner"] = _owner,
                ["repo"] = _repo,
                ["issue_number"] = issueNumber.ToString(),
                ["body"] = message
            };

            var content = new FormUrlEncodedContent(values);
            var response = await Client.PostAsync(endpoint, content);

            response.EnsureSuccessStatusCode();
        }
    }
}
