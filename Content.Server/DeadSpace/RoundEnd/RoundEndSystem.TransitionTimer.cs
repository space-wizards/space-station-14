// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.RoundEnd;

public sealed partial class RoundEndSystem
{
    private CancellationTokenSource? _roundTransitionCancelToken;
    private TimeSpan? _roundTransitionTime;
    private TimeSpan? _pausedRoundTransitionTimeLeft;
    private Action? _roundTransitionCallback;
    private bool _roundTransitionRestartsRound;

    internal bool RoundTransitionRestartsRound => _roundTransitionRestartsRound;

    internal void StartRoundEndTimer(TimeSpan delay)
    {
        StartRoundTransitionTimer(delay, () => EndRound(), false);
    }

    private void StartRoundRestartTimer(TimeSpan delay)
    {
        StartRoundTransitionTimer(delay, AfterEndRoundRestart, true);
    }

    private void StartRoundTransitionTimer(TimeSpan delay, Action callback, bool restartsRound)
    {
        _roundTransitionCancelToken?.Cancel();
        _roundTransitionCancelToken = new();
        _roundTransitionTime = _gameTiming.CurTime + delay;
        _pausedRoundTransitionTimeLeft = null;
        _roundTransitionCallback = callback;
        _roundTransitionRestartsRound = restartsRound;
        Timer.Spawn(delay, OnRoundTransitionTimerElapsed, _roundTransitionCancelToken.Token);
    }

    private void OnRoundTransitionTimerElapsed()
    {
        var callback = _roundTransitionCallback;
        _roundTransitionCancelToken = null;
        _roundTransitionTime = null;
        _pausedRoundTransitionTimeLeft = null;
        _roundTransitionCallback = null;
        _roundTransitionRestartsRound = false;
        callback?.Invoke();
    }

    private void ResetRoundTransitionTimer()
    {
        _roundTransitionCancelToken?.Cancel();
        _roundTransitionCancelToken = null;
        _roundTransitionTime = null;
        _pausedRoundTransitionTimeLeft = null;
        _roundTransitionCallback = null;
        _roundTransitionRestartsRound = false;
    }

    internal bool PauseRoundTransitionTimer()
    {
        if (_roundTransitionTime is not { } endTime)
            return false;

        _pausedRoundTransitionTimeLeft = endTime - _gameTiming.CurTime;
        _roundTransitionTime = null;
        _roundTransitionCancelToken?.Cancel();
        _roundTransitionCancelToken = null;
        return true;
    }

    internal bool ToggleRoundTransitionTimer(out bool paused)
    {
        if (PauseRoundTransitionTimer())
        {
            paused = true;
            return true;
        }

        if (_pausedRoundTransitionTimeLeft is not { } timeLeft || _roundTransitionCallback is not { } callback)
        {
            paused = false;
            return false;
        }

        StartRoundTransitionTimer(timeLeft, callback, _roundTransitionRestartsRound);
        paused = false;
        return true;
    }

    internal bool AdjustRoundTransitionTimer(TimeSpan adjustment)
    {
        if (_roundTransitionTime is { } endTime && _roundTransitionCallback is { } callback)
        {
            StartRoundTransitionTimer(
                endTime - _gameTiming.CurTime + adjustment,
                callback,
                _roundTransitionRestartsRound);
            return true;
        }

        if (_pausedRoundTransitionTimeLeft is not { } timeLeft)
            return false;

        _pausedRoundTransitionTimeLeft = timeLeft + adjustment;
        return true;
    }
}
