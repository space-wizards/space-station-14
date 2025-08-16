namespace Content.Shared.Emp;

/// <summary>
/// If a entity has this component it is destroyed by EMPs
/// </summary>
[RegisterComponent]
[Access(typeof(SharedEmpSystem))]
public sealed partial class DestroyOnEmpComponent : Component
{

}
