using Content.Client.Chemistry.UI;
using Content.Client.Items;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.EntitySystems;

public sealed partial class InjectorStatusControlSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainers = default!;

    public static readonly ProtoId<ItemStatusPrototype> InjectorItemStatus = "Injector";

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<InjectorComponent>(injector => new InjectorStatusControl(injector, _solutionContainers, ProtoMan), InjectorItemStatus);
    }
}
