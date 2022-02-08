using System;
using Content.Server.Atmos;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components;

[RegisterComponent, Friend(typeof(LungSystem))]
public class LungComponent : Component
{
    public float AccumulatedFrametime;

    [ViewVariables]
    public TimeSpan LastGaspPopupTime;

    [DataField("air")]
    public GasMixture Air { get; set; } = new()
    {
        Volume = 6,
        Temperature = Atmospherics.NormalBodyTemperature
    };

    [DataField("gaspPopupCooldown")]
    public TimeSpan GaspPopupCooldown { get; private set; } = TimeSpan.FromSeconds(8);

    [ViewVariables]
    public LungStatus Status { get; set; }

    [DataField("cycleDelay")]
    public float CycleDelay { get; set; } = 2;
}

public enum LungStatus
{
    None = 0,
    Inhaling,
    Exhaling
}
