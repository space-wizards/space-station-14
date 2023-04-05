using Content.Shared.DragDrop;
using Content.Shared.Medical.Cryogenics;

namespace Content.Client.Medical.Cryogenics;

[RegisterComponent, ComponentReference(typeof(SharedCryoPodComponent))]
public sealed class CryoPodComponent : SharedCryoPodComponent { }
