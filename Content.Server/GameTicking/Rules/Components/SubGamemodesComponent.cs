using Content.Server.GameTicking.Rules;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// When this gamerule is added it has a chance of adding other gamerules.
/// Since it's done when added and not when started you can still use normal start logic.
/// Used for starting subgamemodes in game presets.
/// </summary>
[RegisterComponent, Access(typeof(SubGamemodesSystem))]
public sealed partial class SubGamemodesComponent : Component
{
    /// <summary>
    /// Dictionary of each gamerule prototype and the chance of it being added.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<EntProtoId, float> Rules = new();

    /// <summary>
    /// If not null, how many rules this can successfully start.
    /// </summary>
    [DataField]
    public uint? Limit;
}
