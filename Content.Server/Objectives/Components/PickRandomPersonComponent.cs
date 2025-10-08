using Content.Server.Objectives.Systems;
using Content.Shared.Mind.Filters;
using Robust.Shared.Audio;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person from a pool and filters.
/// </summary>
/// <remarks>
/// Don't copy paste this for a new objective, if you need a new filter just make a new filter and set it in YAML.
/// </remarks>
[RegisterComponent, Access(typeof(PickObjectiveTargetSystem))]
public sealed partial class PickRandomPersonComponent : Component
{
    /// <summary>
    /// A pool to pick potential targets from.
    /// </summary>
    [DataField]
    public IMindPool Pool = new AliveHumansPool();

    /// <summary>
    /// Filters to apply to <see cref="Pool"/>.
    /// </summary>
    [DataField]
    public List<MindFilter> Filters = new();

    /// <summary>
    /// Rerolls for a new target if the old target enters cryostorage
    /// </summary>
    [DataField]
    public bool RerollsCryostorage = true;

    /// <summary>
    /// Text displayed once target is rerolled
    /// </summary>
    [DataField]
    public string RerollText = "";

    /// <summary>
    /// Color for the target reroll text
    /// </summary>
    [DataField]
    public Color RerollColor = Color.OrangeRed;

    /// <summary>
    /// Sound played when the target is rerolled
    /// </summary>
    [DataField]
    public SoundSpecifier RerollSound = new SoundPathSpecifier("/Audio/Misc/cryo_warning.ogg");
}
