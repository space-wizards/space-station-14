using Robust.Shared.GameStates;

namespace Content.Shared.Biocoded;

/// <summary>
/// Used for cancelling interactions in case fingerprint of the user doesn't match the provided fingerprint.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BiocodedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Fingerprint;

    [DataField, AutoNetworkedField]
    public bool IgnoreGloves;

    [DataField]
    public LocId? FailPopup = "biocoded-fail-popup";

    [DataField]
    public LocId? FailGlovesPopup = "biocoded-fail-gloves-popup";
}
