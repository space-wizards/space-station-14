using Content.Server.Defusable.Components;

namespace Content.Server.Defusable.Systems;

/// <summary>
/// This handles defusable explosives, such as Syndicate Bombs.
/// </summary>
/// <remarks>
/// i am god's smartest coder
/// - rain
/// (i am obviously joking expect the worst)
/// </remarks>
public sealed class DefusableSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public void StartCountdown(DefusableComponent comp)
    {
        // todo: handle countdown
        // also might want to have admin logs
        if (!comp.BombUsable)
            return;

        // bobm
    }

    public void DetonateBomb(DefusableComponent comp)
    {
        // todo: boom??? lol?
        // also might want to have admin logs
    }

    public void DefuseBomb(DefusableComponent comp)
    {
        // todo: defusing lmfao
        // also might want to have admin logs
        comp.BombLive = false;
    }

    public void Update()
    {
        // todo: handle bombs
    }
}
