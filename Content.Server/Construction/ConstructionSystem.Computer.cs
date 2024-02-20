using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Computer;
using Robust.Shared.Containers;

namespace Content.Server.Construction;

public sealed partial class ConstructionSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private void InitializeComputer()
    {
        SubscribeLocalEvent<ComputerComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ComputerComponent, MapInitEvent>(OnCompMapInit);
        SubscribeLocalEvent<ComputerComponent, PowerChangedEvent>(OnCompPowerChange);
    }

    private void OnCompInit(EntityUid uid, ComputerComponent component, ComponentInit args)
    {
        // Let's ensure the container manager and container are here.
        _container.EnsureContainer<Container>(uid, "board");

        if (TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
        {
            _appearance.SetData(uid, ComputerVisuals.Powered, powerReceiver.Powered);
        }
    }

    private void OnCompMapInit(Entity<ComputerComponent> component, ref MapInitEvent args)
    {
        CreateComputerBoard(component);
    }

    private void OnCompPowerChange(EntityUid uid, ComputerComponent component, ref PowerChangedEvent args)
    {
        _appearance.SetData(uid, ComputerVisuals.Powered, args.Powered);
    }

    /// <summary>
    ///     Creates the corresponding computer board on the computer.
    ///     This exists so when you deconstruct computers that were serialized with the map,
    ///     you can retrieve the computer board.
    /// </summary>
    private void CreateComputerBoard(Entity<ComputerComponent> ent)
    {
        var component = ent.Comp;
        // Ensure that the construction component is aware of the board container.
        if (TryComp<ConstructionComponent>(ent, out var construction))
            AddContainer(ent, "board", construction);

        // We don't do anything if this is null or empty.
        if (string.IsNullOrEmpty(component.BoardPrototype))
            return;

        var container = _container.EnsureContainer<Container>(ent, "board");

        // We already contain a board. Note: We don't check if it's the right one!
        if (container.ContainedEntities.Count != 0)
            return;

        var board = EntityManager.SpawnEntity(component.BoardPrototype, Transform(ent).Coordinates);

        if (!_container.Insert(board, container))
            Log.Warning($"Couldn't insert board {board} to computer {ent}!");
    }
}
