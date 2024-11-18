namespace Content.Shared.Abilities.Goliath;

public abstract class AccUpdateEntitySystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator > Threshold)
        {
            AccUpdate();
            _accumulator = 0;
        }
    }
    protected virtual void AccUpdate()
    {
    }
    protected virtual float Threshold { get; set; } = 0.35f;
    private float _accumulator = 0f;
}
