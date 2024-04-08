using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

/// <summary>                                                                                                                                                 
/// Specifies the paper type (see textures/storage/crates/labels.rsi to see currently supported paper types)  to show on crates this label is attached to.                                                                     
/// </summary>                                                                                                                                                
[RegisterComponent, NetworkedComponent]
public sealed partial class PaperLabelTypeComponent : Component
{
    /// <summary>                                                                                                                                             
    /// The type of label to show.                                                                                                                                    
    /// </summary>
    [DataField]
    public string PaperType = "Paper";
}
