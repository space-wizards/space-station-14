using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Humanoid;

namespace Content.Shared.Changeling;

public sealed partial class LingAbsorbActionEvent : EntityTargetActionEvent
{
}

public sealed partial class LingStingExtractActionEvent : EntityTargetActionEvent
{
}

public sealed partial class LingStingLSDActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class AbsorbDoAfterEvent : SimpleDoAfterEvent
{
}
public sealed partial class ChangelingEvolutionMenuActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingCycleDNAActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent
{
}

public sealed partial class LingRegenerateActionEvent : InstantActionEvent
{
}

public sealed partial class ArmBladeActionEvent : InstantActionEvent
{
}

public sealed partial class LingArmorActionEvent : InstantActionEvent
{
}

public sealed partial class LingInvisibleActionEvent : InstantActionEvent
{
}

public sealed partial class LingEMPActionEvent : InstantActionEvent
{
}

[NetSerializable, Serializable]
public enum ChangelingVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3
}
public struct TransformData
{
    /// <summary>
    /// Name to set your player to when transforming.
    /// </summary>
    public string Name;

    /// <summary>
    /// Fingerprints to use when transforming.
    /// </summary>
    public string Fingerprint;

    /// <summary>
    /// DNA sequence to use when transforming.
    /// </summary>
    public string Dna;

    /// <summary>
    /// Humanoid appearance to use when transforming.
    /// </summary>
    public HumanoidAppearanceComponent HumanoidAppearanceComp;
}