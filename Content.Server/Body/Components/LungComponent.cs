using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components;

[RegisterComponent, Friend(typeof(LungSystem))]
public class LungComponent : Component
{
    public override string Name => "Lung";

    [DataField("air")]
    public GasMixture Air { get; set; } = new()
    {
        Volume = 6,
        Temperature = Atmospherics.NormalBodyTemperature
    };

    [DataField("validReagentGases", required: true)]
    public HashSet<Gas> ValidGases = default!;

    [ViewVariables]
    public Solution LungSolution = default!;
}
