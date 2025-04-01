using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Shared._Impstation.Pudge;

public sealed partial class PudgeMeatHookEvent : WorldTargetActionEvent { }
public sealed partial class PudgeRotEvent : InstantActionEvent { }
public sealed partial class PudgeMeatShieldEvent : InstantActionEvent { }
public sealed partial class PudgeMeatShieldBreakEvent : InstantActionEvent { }
public sealed partial class PudgeDismemberEvent : EntityTargetActionEvent { }

[RegisterComponent]
public sealed partial class MeatShieldComponent : Component { }

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MeatHookComponent : Component
{
    /// <summary>
    /// Hook's reeling force and speed - the higher the number, the faster the hook rewinds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReelRate = 2.5f;

    [DataField("jointId"), AutoNetworkedField]
    public string Joint = string.Empty;

    [DataField, AutoNetworkedField]
    public EntityUid? Projectile;

    [DataField, AutoNetworkedField]
    public bool Reeling;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ReelSound = new SoundPathSpecifier("/Audio/Weapons/reel.ogg")
    {
        Params = AudioParams.Default.WithLoop(true)
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier? CycleSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/kinetic_reload.ogg");

    [DataField]
    public SpriteSpecifier RopeSprite =
        new SpriteSpecifier.Rsi(new ResPath("_Impstation/Objects/Fishing/fishingrod.rsi"), "rope");

    public EntityUid? Stream;
}
