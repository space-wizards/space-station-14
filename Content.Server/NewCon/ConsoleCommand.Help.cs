namespace Content.Server.NewCon;

public abstract partial class ConsoleCommand
{
    public string Description(string? subCommand)
        => Loc.GetString($"command-description-{Name}" + (subCommand is not null ? $"-{subCommand}" : ""));

    public override string ToString()
    {
        return $"{Name}: {Description(null)}";
    }
}
