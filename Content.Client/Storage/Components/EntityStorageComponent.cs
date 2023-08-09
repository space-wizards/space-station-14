using Content.Shared.Storage.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Storage.Components;

[RegisterComponent, ComponentReference(typeof(SharedEntityStorageComponent))]
public sealed class EntityStorageComponent : SharedEntityStorageComponent
{

}
