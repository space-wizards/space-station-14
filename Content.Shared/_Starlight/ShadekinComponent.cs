using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ShadekinComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> ShadekinAlert = "Shadekin";

    [ViewVariables(VVAccess.ReadOnly), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1f);

    [ViewVariables(VVAccess.ReadOnly)]
    public float LightExposure = 0;
}

public sealed partial class ShadekinAlertEvent : BaseAlertEvent;