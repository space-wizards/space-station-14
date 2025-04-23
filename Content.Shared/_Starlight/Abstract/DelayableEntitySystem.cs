namespace Content.Shared.Abilities.Goliath;

public abstract class DelayableEntitySystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator > Threshold)
        {
            _isOdd = !_isOdd;
            _accumulator = 0;
            while ((_isOdd ? _oddQueue : _evenQueue).TryDequeue(out var action))
                action();
        }
    }
    protected virtual float Threshold { get; set; } = 0.35f;
    private readonly Queue<Action> _oddQueue = new();
    private readonly Queue<Action> _evenQueue = new();
    private bool _isOdd = true;
    private float _accumulator = 0f;

    protected void EnqueueNext(Action action)
    {
        if (!_isOdd)
            _oddQueue.Enqueue(action);
        else
            _evenQueue.Enqueue(action);
    }
}
