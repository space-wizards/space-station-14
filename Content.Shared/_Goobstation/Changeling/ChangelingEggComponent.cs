using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

[RegisterComponent, NetworkedComponent]

public sealed partial class ChangelingEggComponent : Component
{
    public ChangelingComponent lingComp;
    public EntityUid lingMind;
    public StoreComponent lingStore;

    /// <summary>
    ///     Countdown before spawning monkey.
    /// </summary>
    public TimeSpan UpdateTimer = TimeSpan.Zero;
    public float UpdateCooldown = 60f;
    public bool active = false;
}
