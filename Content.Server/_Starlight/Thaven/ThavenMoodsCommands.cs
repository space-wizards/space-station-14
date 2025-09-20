using System.Linq;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Preferences.Managers;
using Content.Shared._Starlight.Thaven;
using Content.Shared._Starlight.Thaven.Components;
using Content.Shared.Administration;
using Content.Shared.Dataset;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._Starlight.Thaven;

[AdminCommand(AdminFlags.Admin)]
internal sealed class ThavenSharedMoodsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entman = default!;
    private ThavenMoodsSystem? _moods;
    public override string Command => "thavenshared";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _moods ??= _entman.GetEntitySystem<ThavenMoodsSystem>();
        var moods = _moods.SharedMoods;
        foreach (var mood in moods)
        {
            shell.WriteLine($"{mood.GetLocName()}: {mood.GetLocDesc()}");
        }
    }
}

[AdminCommand(AdminFlags.Admin)]
internal sealed class ThavenRerollMoodsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _entman = default!;
    private ThavenMoodsSystem? _moods;
    public override string Command => "thavenreollshared";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _moods ??= _entman.GetEntitySystem<ThavenMoodsSystem>();
        _moods.NewSharedMoods();
    }
}

[ToolshedCommand(Name = "moods"), AdminCommand(AdminFlags.Admin)]
public sealed class AdminMoodsCommand : ToolshedCommand
{
    private ThavenMoodsSystem? _moods;

    [CommandImplementation("addproto")]
    public EntityUid AddMoodProto(
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<ThavenMoodPrototype> mood
    )
    {
        _moods ??= GetSys<ThavenMoodsSystem>();

        if (TryComp<ThavenMoodsComponent>(input, out var moodsComponent))
        {
            _moods.TryAddMood((input, moodsComponent), mood);
        }

        return input;
    }

    [CommandImplementation("addproto")]
    public IEnumerable<EntityUid> AddMoodProto(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<ThavenMoodPrototype> language
    ) => input.Select(x => AddMoodProto(x, language));

    [CommandImplementation("adddataset")]
    public EntityUid AddMoodDataset(
        [PipedArgument] EntityUid input,
        [CommandArgument] ProtoId<DatasetPrototype> dataset
    )
    {
        _moods ??= GetSys<ThavenMoodsSystem>();

        if (TryComp<ThavenMoodsComponent>(input, out var moodsComponent))
        {
            var ent = (input, moodsComponent);
            if (_moods.TryPick(dataset, out var proto, _moods.GetActiveMoods(ent)))
                _moods.TryAddMood(ent, proto, true, false);
        }

        return input;
    }

    [CommandImplementation("adddataset")]
    public IEnumerable<EntityUid> AddMoodDataset(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] ProtoId<DatasetPrototype> dataset
    ) => input.Select(x => AddMoodDataset(x, dataset));

    [CommandImplementation("addraw")]
    public EntityUid AddMoodRaw(
        [PipedArgument] EntityUid input,
        [CommandArgument] string title,
        [CommandArgument] string description
    )
    {
        _moods ??= GetSys<ThavenMoodsSystem>();

        if (TryComp<ThavenMoodsComponent>(input, out var moodsComponent))
        {
            var ent = (input, moodsComponent);
            var mood = new ThavenMood();
            mood.MoodName = title;
            mood.MoodDesc = description;
            _moods.AddMood(ent, mood);
        }

        return input;
    }

    [CommandImplementation("addraw")]
    public IEnumerable<EntityUid> AddMoodDataset(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] string title,
        [CommandArgument] string description
    ) => input.Select(x => AddMoodRaw(x, title, description));

    [CommandImplementation("ensure")]
    public EntityUid EnsureMood(
        [PipedArgument] EntityUid input
    )
    {
        _moods ??= GetSys<ThavenMoodsSystem>();

        if (!EntityManager.EnsureComponent<ThavenMoodsComponent>(input, out var moods))
            _moods.NotifyMoodChange((input, moods));

        return input;
    }

    [CommandImplementation("ensure")]
    public IEnumerable<EntityUid> EnsureMood(
        [PipedArgument] IEnumerable<EntityUid> input
    ) => input.Select(x => EnsureMood(x));

    [CommandImplementation("rm")]
    public EntityUid RemoveMood(
    [PipedArgument] EntityUid input,

    [CommandArgument] int index = 0
    )
    {
        _moods ??= GetSys<ThavenMoodsSystem>();

        if (TryComp<ThavenMoodsComponent>(input, out var moods))
            _moods.RemoveMood((input, moods), index);

        return input;
    }

    [CommandImplementation("rm")]
    public IEnumerable<EntityUid> RemoveMood(
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] int index = 0
    ) => input.Select(x => RemoveMood(x, index));
}