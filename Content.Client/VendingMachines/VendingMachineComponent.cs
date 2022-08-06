using Content.Shared.VendingMachines;

namespace Content.Client.VendingMachines;

[RegisterComponent]
[ComponentReference(typeof(SharedVendingMachineComponent))]
public sealed class VendingMachineComponent : SharedVendingMachineComponent
{

}
