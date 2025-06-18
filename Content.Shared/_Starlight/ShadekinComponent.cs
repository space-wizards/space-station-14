using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight;

[RegisterComponent]
public sealed partial class ShadekinComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> ShadekinAlert = "Shadekin";

    public float Accumulator;

    [DataField]
    public float LightExposure = 0;
}

public sealed partial class ShadekinAlertEvent : BaseAlertEvent;