namespace Content.Server.Medical.Surgery;

[RegisterComponent]
public sealed class SurgeryRealmCameraComponent : Component
{
    [ViewVariables] public EntityUid? OldEntity;

    [ViewVariables] public Mind.Mind? Mind;
}
