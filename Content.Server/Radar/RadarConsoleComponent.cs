using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Radar;

[RegisterComponent]
public class RadarConsoleComponent : Component
{
    public override string Name => "RadarConsole";
}

