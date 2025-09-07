using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Chemistry.Components;

[RegisterComponent]
public sealed partial class InjectOnCollideComponent : Component
{
    [DataField("reagents")]
    public List<ReagentQuantity> Reagents;
    
    [DataField("limit")]
    public float? ReagentLimit;

    [DataField("sound")]
    public SoundSpecifier? Sound;
}