using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Placeable;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Temperature.Systems;

/// <summary>
/// This is a central system to streamline the process of accessing heat containers.
/// It takes care of resolving queries by using standardized methods for:
/// 1. addressing a specific heat container within an entity tree.
/// 2. exposing heat containers in an entity to an outside observer.
/// </summary>
/// <remarks>To quote @ ArtisticRoomba
/// "An entity that has a HeatContainer (or IHeatContainer), that HeatContainer is going to be serving some specific purpose that is specific to the system."
/// This Query System is about controlling how a thermodynamic system of one entity is allowed to interact with another or help components interfacing with heat containers within their system more seamlessly.
/// </remarks>
public sealed partial class HeatContainerQuerySystem : EntitySystem
{
    [Robust.Shared.IoC.Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Robust.Shared.IoC.Dependency] private SharedContainerSystem _containers = default!;
    [Robust.Shared.IoC.Dependency] private IPrototypeManager _prototypeManager = default!;
    [Robust.Shared.IoC.Dependency] private EntityQuery<SolutionManagerComponent> _solutionsManagerQuery = default!;
    [Robust.Shared.IoC.Dependency] private EntityQuery<HeatableComponent> _heatablesQuery = default!;
    [Robust.Shared.IoC.Dependency] private EntityQuery<TemperatureComponent> _temperatureQuery = default!;
    [Robust.Shared.IoC.Dependency] private EntityQuery<SolutionComponent> _solutionQuery = default!;
    [Robust.Shared.IoC.Dependency] private SharedAtmosphereSystem _atmosphere = default!;

    /// <summary>
    /// An address to a heat container. Shared between relevant components.
    /// </summary>
    [DataDefinition]
    public sealed partial class HeatContainerAddress : IEquatable<HeatContainerAddress>
    {
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)AddressType;
                hashCode = (hashCode * 397) ^ TargetName.GetHashCode();
                hashCode = (hashCode * 397) ^ IncludeExternals.GetHashCode();
                hashCode = (hashCode * 397) ^ (Next != null ? Next.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is HeatContainerAddress other && Equals(other);
        }



        public enum TargetType
        {
            None,
            ContainerSlot,
            Component,
            Solution,
        }

        [DataField(required: true)]
        public TargetType AddressType { get; init; }

        /// <summary>
        /// Name of the container, component or solution addressed.
        /// </summary>
        [DataField(required: true)]
        public string TargetName { get; init; }

        /// <summary>
        /// Should the target forward request to entities considered outside the current entity tree?
        /// Best case is <see cref="ItemPlacerComponent"/> which could cause a loop if an entity placed on top of it, also has this component.
        /// A <see cref="HeatableComponent"/> should never have an address with externals include to avoid loops in the search tree.
        /// </summary>
        [DataField]
        public bool IncludeExternals { get; init; }

        /// <summary>
        /// What container should be searched for next.
        /// </summary>
        [DataField]
        public HeatContainerAddress? Next { get; init; }

        public override string ToString()
        {
            return
                $"{Enum.GetName(AddressType)}: {TargetName} ({IncludeExternals.ToString()}){(Next == null ? "" : " -> " + Next.ToString())}";
        }

        public bool Equals(HeatContainerAddress? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return AddressType == other.AddressType && TargetName == other.TargetName &&
                   IncludeExternals == other.IncludeExternals && (Next?.Equals(other.Next) ?? other.Next == null);
        }

        public static bool operator ==(HeatContainerAddress? left, HeatContainerAddress? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HeatContainerAddress? left, HeatContainerAddress? right)
        {
            return !Equals(left, right);
        }
    }


    /// <summary>
    /// Get container by their address in an entity.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="entityUid">entity to search</param>
    /// <returns>a list of all containers. remember to apply them again after changes</returns>
    /// <param name="assertThatFound"></param>
    /// <seealso cref="ApplyHeatContainer"/>
    public IEnumerable<IHeatContainer> FindContainer(HeatContainerAddress address,
        EntityUid entityUid,
        bool assertThatFound = true)
    {
        switch (address.AddressType)
        {
            case HeatContainerAddress.TargetType.None:
                return [];
            case HeatContainerAddress.TargetType.Solution:
                if (_solutions.TryGetSolution(entityUid, address.TargetName, out var solution))
                {
                    GetContainerFromComponent(solution.Value.Owner, solution.Value.Comp, false);
                }

                break;
            case HeatContainerAddress.TargetType.ContainerSlot:
                if (_containers.TryGetContainer(entityUid, address.TargetName!, out var container))
                {
                    if (address.Next == null)
                        return container.ContainedEntities.SelectMany(EnumerateContainersInEntity);
                    else
                        return container.ContainedEntities.SelectMany(e => FindContainer(address.Next, e, false));
                }

                break;
            case HeatContainerAddress.TargetType.Component:
                if (Factory.TryGetRegistration(address.TargetName, out var registration))
                {
                    if (TryComp(entityUid, registration.Type, out var component))
                        return GetContainerFromComponent(entityUid, component, address.IncludeExternals);
                }
                else
                {
                    Log.Error(
                        $"{nameof(FindContainer)} failed: Unknown component name {address.TargetName} passed as part of address!");
                }


                break;
        }

        if (assertThatFound)
        {
            throw new Exception("Invalid Address: " + address.ToString());
        }

        return [];
    }

    /// <summary>
    /// Lists all heat container to a component.
    /// </summary>
    /// <param name="entityUid"></param>
    /// <param name="component"></param>
    /// <param name="includeExternals"></param>
    /// <returns></returns>
    public IEnumerable<IHeatContainer> GetContainerFromComponent(EntityUid entityUid,
        IComponent component,
        bool includeExternals)
    {
        switch (component)
        {
            case IHeatContainer heatContainer:
                return [heatContainer];
            //if solution gets IHeatContainer, we never reach this case. Future Proof! This goes for any other component below.
            case SolutionComponent slnComp:
                var capacity = slnComp.Solution.GetHeatCapacity(_prototypeManager);
                return
                [
                    new HeatContainer.BoxedHeatContainer(entityUid,
                        component,
                        capacity,
                        slnComp.Solution.Temperature)
                ];
            case ItemPlacerComponent itemPlacer:
                return includeExternals ? itemPlacer.PlacedEntities.SelectMany(EnumerateContainersInEntity) : [];
            case GasMaxPressureHolderComponent gasMaxPressureHolder:
                return
                [
                    new HeatContainer.BoxedHeatContainer(entityUid,
                        component,
                        _atmosphere.GetHeatCapacity(gasMaxPressureHolder.Air, false),
                        gasMaxPressureHolder.Air.Temperature)
                ];
        }

        return [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyBoxedContainers(IEnumerable<IHeatContainer> containers)
    {
        foreach (var container in containers)
        {
            if (container is BoxedHeatContainer boxed)
                ApplyBoxedContainer(boxed);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyHeatContainer(IHeatContainer container)
    {
        if (container is BoxedHeatContainer boxed)
            ApplyBoxedContainer(boxed);
    }

    public void ApplyBoxedContainer(BoxedHeatContainer container)
    {
        switch (container.Component)
        {
            case SolutionComponent slnComp:
                slnComp.Solution.Temperature = container.Temperature;
                Dirty(container.EntityUid, container.Component);
                break;
            case GasMaxPressureHolderComponent gasMaxPressureHolder:
                //TODO check: do i need to adjust pressure?
                gasMaxPressureHolder.Air.Temperature = container.Temperature;
                Dirty(container.EntityUid, container.Component);
                break;
        }
    }

    /// <summary>
    /// Using the <see cref="HeatableComponent"/> of an entity, get the containers marked for heat exchanges.
    /// </summary>
    /// <param name="entityUid"></param>
    /// <returns></returns>
    /// <remarks>if there are ever any other components used to negotiate contacts, for example: exposed while state XYZ, add them here.</remarks>
    public IEnumerable<IHeatContainer> EnumerateContainersInEntity(EntityUid entityUid)
    {
        //check for heatable
        if (_heatablesQuery.TryComp(entityUid, out var heatable))
            return heatable.ExposedContainers.SelectMany(e => FindContainer(e, entityUid));
        //fallback to temperature, since it is so far always used as a simple way for heating non solution items.
        // food uses it primarily, which hopefully gets removed once solutions are heat containers and there are proper ways to integrate their heat change into construction graph.
        if (_temperatureQuery.TryComp(entityUid, out var temperatureComponent))
            return [temperatureComponent];

        return [];
    }
}
