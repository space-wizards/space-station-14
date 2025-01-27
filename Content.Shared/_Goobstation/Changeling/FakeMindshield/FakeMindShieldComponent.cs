using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared._Goobstation.FakeMindshield.Components;

/// <summary>
/// A fake Mindshield solely to trick HUDs, with no other effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FakeMindShieldComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";
}
