using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server.NewCon.Commands.Info;

[ConsoleCommand]
public sealed class BuildInfoCommand : ConsoleCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly string Gold = Color.Gold.ToHex();

    [CommandImplementation]
    public void BuildInfo([CommandInvocationContext] IInvocationContext ctx)
    {
        var game = _cfg.GetCVar(CVars.BuildForkId);
        ctx.WriteLine(FormattedMessage.FromMarkup($"[color={Gold}]Game:[/color] {game}"));
        var buildCommit = _cfg.GetCVar(CVars.BuildHash);
        ctx.WriteLine(FormattedMessage.FromMarkup($"[color={Gold}]Build commit:[/color] {buildCommit}"));
        var buildManifest = _cfg.GetCVar(CVars.BuildManifestHash);
        ctx.WriteLine(FormattedMessage.FromMarkup($"[color={Gold}]Manifest hash:[/color] {buildManifest}"));
        var engine = _cfg.GetCVar(CVars.BuildEngineVersion);
        ctx.WriteLine(FormattedMessage.FromMarkup($"[color={Gold}]Engine ver:[/color] {engine}"));
    }
}
