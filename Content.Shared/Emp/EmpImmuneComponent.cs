namespace Content.Shared.Emp;

/// <summary>
/// The entity with this component is not affected by Emp.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedEmpSystem))]
public sealed partial class EmpImmuneComponent : Component
{

}