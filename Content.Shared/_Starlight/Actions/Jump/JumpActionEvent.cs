using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared._Starlight.Actions.Jump;
  
[Virtual]
public partial class JumpActionEvent : WorldTargetActionEvent
{
    [DataField]
    public float Distance = 5f;

    [DataField]
    public bool ToPointer = true;

    [DataField]
    public bool FromGrid = true;

    [DataField]
    public float Speed = 15F;

    [DataField]
    public SoundSpecifier? Sound = default;
}

public sealed partial class JetJumpActionEvent : JumpActionEvent
{
    [DataField]
    public float MoleUsage = 0.24f; // 20x
}