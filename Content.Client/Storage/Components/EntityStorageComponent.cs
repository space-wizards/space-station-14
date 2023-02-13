using Content.Shared.Storage.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Storage.Components;

[RegisterComponent, NetworkedComponent, ComponentReference(typeof(SharedEntityStorageComponent))]
public sealed class EntityStorageComponent : SharedEntityStorageComponent
{

}
