using Content.Server.Github.Requests;
using System.Threading.Tasks;
using Content.Server.BugReports;

namespace Content.Server.Github;

public sealed class GithubApiManager
{
    [Dependency] private readonly GithubBackgroundWorker _githubWorker = default!;

    public void Initialize()
    {
        Task.Run(() => _githubWorker.HandleQueue());
    }

    public bool TryCreateIssue(ValidPlayerBugReportReceivedEvent bugReport)
    {
        var createIssueRequest = ConvertToCreateIssue(bugReport);
        return TryMakeRequest(createIssueRequest);
    }

    public bool TryMakeRequest(IGithubRequest request)
    {
        return _githubWorker.Writer.TryWrite(request);
    }

    private CreateIssueRequest ConvertToCreateIssue(ValidPlayerBugReportReceivedEvent bugReport)
    {
        var request = new CreateIssueRequest
        {
            Title = bugReport.Title,
            Labels = bugReport.Tags,
        };

        var metadata = bugReport.MetaData;

        request.Body = Loc.GetString("github-issue-format",
            ("description", bugReport.Description),
            ("buildVersion", metadata.BuildVersion),
            ("engineVersion", metadata.EngineVersion),
            ("serverName", metadata.ServerName),
            ("submittedTime", metadata.SubmittedTime),
            ("roundNumber", metadata.RoundNumber.ToString() ?? ""),
            ("roundTime", metadata.RoundTime.ToString() ?? ""),
            ("roundType", metadata.RoundType ?? ""),
            ("map", metadata.Map ?? ""),
            ("numberOfPlayers", metadata.NumberOfPlayers),
            ("username", metadata.Username),
            ("playerGUID", metadata.PlayerGUID));

        return request;
    }
}
