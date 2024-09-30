using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CombatMode.Pacification;

/// <summary>
/// Status effect that disallows harming living things and restricts aggressive actions.
///
/// There is a caveat with pacifism. It's not intended to be wholly encompassing: there are ways of harming people
/// while pacified--plenty of them, even! The goal is to restrict the obvious ones to make gameplay more interesting
/// while not overly limiting.
///
/// If you want full-pacifism (no combat mode at all), you can simply set <see cref="DisallowAllCombat"/> before adding.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(PacificationSystem))]
public sealed partial class PacifiedComponent : Component
{
    [DataField]
    public bool DisallowDisarm = false;

    /// <summary>
    ///  If true, this will disable combat entirely instead of only disallowing attacking living creatures and harmful things.
    /// </summary>
    [DataField]
    public bool DisallowAllCombat = false;


    /// <summary>
    ///     When attempting attack against the same entity multiple times,
    ///     don't spam popups every frame and instead have a cooldown.
    /// </summary>
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(3.0);

    [DataField]
    [AutoPausedField]
    public TimeSpan? NextPopupTime = null;

    /// <summary>
    ///     The last entity attacked, used for popup purposes (avoid spam)
    /// </summary>
    [DataField]
    public EntityUid? LastAttackedEntity = null;

    [DataField]
    public ProtoId<AlertPrototype> PacifiedAlert = "Pacified";
}
