using Robust.Shared.GameStates;

namespace Content.Shared.HotPotato;

/// <summary>
/// Added to an activated hot potato. Controls hot potato transfer on server / effect spawning on client.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ActiveHotPotatoComponent : Component
{
    public float EffectCooldown = 0.3f;
    public TimeSpan TargetTime = TimeSpan.Zero;
}
