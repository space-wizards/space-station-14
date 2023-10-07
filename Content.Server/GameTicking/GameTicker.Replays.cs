using Content.Shared.CCVar;
using Robust.Shared;
using Robust.Shared.ContentPack;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [Dependency] private readonly IReplayRecordingManager _replays = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    private ISawmill _sawmillReplays = default!;

    private void InitializeReplays()
    {
        _replays.RecordingFinished += ReplaysOnRecordingFinished;
    }

    /// <summary>
    /// A round has started: start recording replays if auto record is enabled.
    /// </summary>
    private void ReplayStartRound()
    {
        try
        {
            if (!_cfg.GetCVar(CCVars.ReplayAutoRecord))
                return;

            if (_replays.IsRecording)
            {
                _sawmillReplays.Warning("Already an active replay recording before the start of the round, not starting automatic recording.");
                return;
            }

            _sawmillReplays.Debug($"Starting replay recording for round {RoundId}");

            var finalPath = GetAutoReplayPath();
            var recordPath = finalPath;
            var tempDir = _cfg.GetCVar(CCVars.ReplayAutoRecordTempDir);
            ResPath? moveToPath = null;

            if (!string.IsNullOrEmpty(tempDir))
            {
                var baseReplayPath = new ResPath(_cfg.GetCVar(CVars.ReplayDirectory)).ToRootedPath();
                moveToPath = baseReplayPath / finalPath;

                var fileName = finalPath.Filename;
                recordPath = new ResPath(tempDir) / fileName;

                _sawmillReplays.Debug($"Replay will record in temporary position: {recordPath}");
            }

            var recordState = new ReplayRecordState(moveToPath);

            if (!_replays.TryStartRecording(_resourceManager.UserData, recordPath.ToString(), state: recordState))
            {
                _sawmillReplays.Error("Can't start automatic replay recording!");
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error while starting an automatic replay recording:\n{e}");
        }
    }

    /// <summary>
    /// A round has ended: stop recording replays and make sure they're moved to the correct spot.
    /// </summary>
    private void ReplayEndRound()
    {
        try
        {
            if (_replays.ActiveRecordingState is ReplayRecordState)
            {
                _replays.StopRecording();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error while stopping replay recording:\n{e}");
        }
    }

    private void ReplaysOnRecordingFinished(ReplayRecordingFinished data)
    {
        if (data.State is not ReplayRecordState state)
            return;

        if (state.MoveToPath == null)
            return;

        _sawmillReplays.Info($"Moving replay into final position: {state.MoveToPath}");
        _taskManager.BlockWaitOnTask(_replays.WaitWriteTasks());
        DebugTools.Assert(!_replays.IsWriting());
        data.Directory.CreateDir(state.MoveToPath.Value.Directory);
        data.Directory.Rename(data.Path, state.MoveToPath.Value);
    }

    private ResPath GetAutoReplayPath()
    {
        var cfgValue = _cfg.GetCVar(CCVars.ReplayAutoRecordName);

        var time = DateTime.UtcNow;

        var interpolated = cfgValue
            .Replace("{year}", time.Year.ToString("D4"))
            .Replace("{month}", time.Month.ToString("D2"))
            .Replace("{day}", time.Day.ToString("D2"))
            .Replace("{hour}", time.Hour.ToString("D2"))
            .Replace("{minute}", time.Minute.ToString("D2"))
            .Replace("{round}", RoundId.ToString());

        return new ResPath(interpolated);
    }

    private sealed record ReplayRecordState(ResPath? MoveToPath);
}
