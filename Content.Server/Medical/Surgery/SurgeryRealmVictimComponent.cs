namespace Content.Server.Medical.Surgery;

[RegisterComponent]
[Access(typeof(SurgeryRealmSystem))]
public sealed class SurgeryRealmVictimComponent : Component
{
    [ViewVariables] public EntityUid Heart;
}
