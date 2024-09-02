namespace Content.Server.Salvage.Magnet;

// This is dumb
/// <summary>
/// Deletes the attached entity if the linked entity is deleted.
/// </summary>
[RegisterComponent]
public sealed partial class SalvageMobRestrictionsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid LinkedEntity;
}
