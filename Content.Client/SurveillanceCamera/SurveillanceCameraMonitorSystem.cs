namespace Content.Client.SurveillanceCamera;

public sealed class SurveillanceCameraMonitorSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, CameraSwitchTiming> _activeTimers = new();
    private readonly List<EntityUid> _toRemove = new();

    private const float InitialTime = 30;

    public override void Update(float frameTime)
    {
        foreach (var (uid, timing) in _activeTimers)
        {
            timing.TimeLeft -= frameTime;

            if (timing.TimeLeft <= 0)
            {
                _toRemove.Add(uid);
            }
        }

        foreach (var uid in _toRemove)
        {
            _activeTimers[uid].OnFinish();
            _activeTimers.Remove(uid);
        }

        _toRemove.Clear();
    }

    public void AddTimer(EntityUid uid, Action onFinish)
    {
        var timing = new CameraSwitchTiming(InitialTime, onFinish);
        _activeTimers[uid] = timing;
    }

    public void RemoveTimer(EntityUid uid)
    {
        _activeTimers.Remove(uid);
    }

    private sealed class CameraSwitchTiming
    {
        public float TimeLeft;
        public Action OnFinish;

        public CameraSwitchTiming(float timeLeft, Action onFinish)
        {
            TimeLeft = timeLeft;
            OnFinish = onFinish;
        }
    }
}
