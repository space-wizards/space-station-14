using Robust.Shared.ContentPack;
using System.Threading.Tasks;
using Content.Replay.UI.Loading;
using Robust.Shared.CPUJob.JobQueues;

namespace Content.Replay.Manager;

public sealed class LoadReplayJob : Job<ReplayData>
{
    private ReplayManager _manager;
    private readonly IWritableDirProvider _dir;
    private readonly LoadingScreen<ReplayData> _screen;

    public LoadReplayJob(float time, IWritableDirProvider dir,
        ReplayManager manager,
        LoadingScreen<ReplayData> screen)
        : base(time, default)
    {
        _manager = manager;
        _dir = dir;
        _screen = screen;
    }

    protected override async Task<ReplayData?> Process()
    {
        var data = await _manager.InternalLoadReplay(_dir, Yield);
        await _manager.StartReplayAsync(data, Yield);
        return data;
    }

    private async Task Yield(float value, float maxValue, LoadingState state, bool force)
    {
        var header = Loc.GetString("replay-loading", ("cur", (int)state + 1), ("total", 5));
        var subText = Loc.GetString(state switch
        {
            LoadingState.LoadingFiles => "replay-loading-reading",
            LoadingState.ProcessingFiles => "replay-loading-processing",
            LoadingState.Spawning => "replay-loading-spawning",
            LoadingState.Initializing => "replay-loading-initializing",
            _ => "replay-loading-starting",
        });
        _screen.UpdateProgress(value, maxValue, header, subText);

        if (force)
            await SuspendNow();
        else
            await SuspendIfOutOfTime();
    }

    public enum LoadingState : byte
    {
        LoadingFiles,
        ProcessingFiles,
        Spawning,
        Initializing,
        Starting,
    }
}
