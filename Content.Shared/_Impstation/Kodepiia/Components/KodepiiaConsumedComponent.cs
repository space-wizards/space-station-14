using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Kodepiia.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class KodepiiaConsumedComponent : Component
{
    [DataField]
    public int TimesConsumed;
}
