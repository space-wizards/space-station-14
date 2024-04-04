using Content.Shared.CrystallPunk.LockKey;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CrystallPunk.LockKey;

/// <summary>
/// A component of a lock that stores its keyhole shape, complexity, and current state.
/// </summary>
[RegisterComponent]
public sealed partial class CPLockpickComponent : Component
{
    [DataField]
    public int Health = 3;

    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/_CP14/Items/lockpick_use.ogg")
    {
        Params = AudioParams.Default
        .WithVariation(0.05f)
        .WithVolume(0.5f)
    };

    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/_CP14/Items/lockpick_fail.ogg")
    {
        Params = AudioParams.Default
        .WithVariation(0.05f)
        .WithVolume(0.5f)
    };
}
