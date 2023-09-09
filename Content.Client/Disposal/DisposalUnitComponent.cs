using Content.Shared.Disposal.Components;

namespace Content.Client.Disposal;

[RegisterComponent]
[ComponentReference(typeof(SharedDisposalUnitComponent))]
public sealed partial class DisposalUnitComponent : SharedDisposalUnitComponent
{

}
