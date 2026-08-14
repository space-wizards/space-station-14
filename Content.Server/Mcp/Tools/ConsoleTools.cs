#if !FULL_RELEASE || MCP
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server.Mcp.Tools;

public sealed class RunCommandTool(McpContext ctx) : McpTool(ctx)
{
    public override string Name => "run_command";

    public override string Description =>
        "Executes a server console command with full host permissions and returns its output. The escape hatch " +
        "for everything without a dedicated tool: variantize, tilewalls, fixrotations, tp, tpgrid, " +
        "aghost, vv, Toolshed expressions and more. " +
        "Pass 'player' for commands that act on a player (tp, aghost...).";

    public override JsonObject InputSchema => Schema.Object(new JsonObject
        {
            ["command"] = Schema.String("Full command line, e.g. 'fixgridatmos 4' or 'tp 0 0 2'."),
            ["player"] = Schema.String("Run in this player's context (default: the server console, no player)."),
        },
        "command");

    public override Task<JsonNode> ExecuteAsync(JsonObject args)
    {
        var commandLine = McpContext.GetString(args, "command").Trim();
        if (commandLine.Length == 0)
            throw new McpToolException("Empty command.");
        var playerName = McpContext.OptString(args, "player");

        return Ctx.RunOnMainThread<JsonNode>(() =>
        {
            ICommonSession? session = null;
            if (playerName != null)
                session = Ctx.ResolveSession(playerName);

            var shell = new CapturingConsoleShell(Ctx.ConsoleHost, session);
            shell.ExecuteCommand(commandLine);

            return (JsonNode) new JsonObject
            {
                ["command"] = commandLine,
                ["output"] = shell.Output,
                ["had_errors"] = shell.HadErrors,
            };
        });
    }
}

/// <summary>
/// A console shell that captures command output instead of writing it to the system console,
/// and executes commands directly (bypassing permission checks — the MCP endpoint is trusted).
/// </summary>
public sealed class CapturingConsoleShell(IConsoleHost host, ICommonSession? player) : IConsoleShell
{
    private readonly StringBuilder _output = new();
    private int _depth;

    public string Output => _output.ToString().TrimEnd();
    public bool HadErrors { get; private set; }

    public IConsoleHost ConsoleHost => host;
    public bool IsServer => true;
    public bool IsLocal => player == null;
    public ICommonSession? Player => player;

    public void ExecuteCommand(string command)
    {
        if (_depth > 8)
        {
            WriteError($"Command recursion too deep, not executing '{command}'.");
            return;
        }

        var args = new List<string>();
        CommandParsing.ParseArguments(command, args);
        if (args.Count == 0)
            return;

        var commandName = args[0];
        if (!host.AvailableCommands.TryGetValue(commandName, out var cmd))
        {
            // Not a regular console command — fall through to Toolshed (entities, self,
            // comp ...), mirroring ServerConsoleHost.ExecuteCommand.
            ExecuteToolshedCommand(command);
            return;
        }

        var argStr = command.Length > commandName.Length ? command[(commandName.Length + 1)..] : "";
        _depth++;
        try
        {
            cmd.Execute(this, argStr, args.GetRange(1, args.Count - 1).ToArray());
        }
        finally
        {
            _depth--;
        }
    }

    private void ExecuteToolshedCommand(string command)
    {
        var toolshed = IoCManager.Resolve<ToolshedManager>();
        toolshed.InvokeCommand(this, command, null, out var res, out var ctx);

        var anyErrors = false;
        foreach (var err in ctx.GetErrors())
        {
            anyErrors = true;
            WriteLine(err.Describe());
        }

        if (anyErrors)
        {
            HadErrors = true;
            return;
        }

        if (res != null)
            WriteLine(toolshed.PrettyPrintType(res, out _, moreUsed: true));
    }

    public void RemoteExecuteCommand(string command)
    {
        // Server side: "remote" means sending the command to the player's client (e.g. mappingclientsidesetup).
        if (player != null)
            host.RemoteExecuteCommand(player, command);
        else
            WriteLine($"(skipped client-side command '{command}': no player in this context)");
    }

    public void WriteLine(string text)
    {
        _output.AppendLine(text);
    }

    public void WriteLine(FormattedMessage message)
    {
        _output.AppendLine(message.ToString());
    }

    public void WriteError(string text)
    {
        HadErrors = true;
        _output.Append("ERROR: ").AppendLine(text);
    }

    public void Clear()
    {
        _output.Clear();
    }
}

#endif
