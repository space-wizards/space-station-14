using Content.Server.Antag.Systems;
using Content.Shared.Antag;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

/// <summary>
/// Makes a player an antagonist. If the antagonist depends on gameRule,
/// it will not work correctly if it has not been added earlier.
/// </summary>
[RegisterComponent, Access(typeof(AutoAntagSystem))]
public sealed partial class AutoAntagComponent : Component
{
    /// <summary>
    /// The used AntagLoadout.
    /// </summary>
    [DataField]
    public ProtoId<AntagLoadoutPrototype> AntagLoadout;
}
