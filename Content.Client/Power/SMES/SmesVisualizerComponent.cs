namespace Content.Client.Power.SMES;

[RegisterComponent]
public sealed class SmesVisualsComponent : Component
{
    /// <summary>
    /// 
    /// </summary>
    [DataField("chargeOverlayPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string ChargeOverlayPrefix = "smes-og";
    
    /// <summary>
    /// 
    /// </summary>
    [DataField("inputOverlayPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string InputOverlayPrefix = "smes-oc";
    
    /// <summary>
    /// 
    /// </summary>
    [DataField("outputOverlayPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string OutputOverlayPrefix = "smes-op";
}
