using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components.Solutions;

/// <summary>
/// <para>Holds the composition of an entity made from reagents and its reagent temperature.</para>
/// <para>If the entity is used to represent a collection of reagents inside of a container such as a beaker, syringe, bloodstream, food, or similar the entity is tracked by a <see cref="SolutionContainerManagerComponent"/> on the container and has a <see cref="ContainedSolutionComponent"/> tracking which container it's in.</para>
/// </summary>
/// <remarks>
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SolutionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Parent = EntityUid.Invalid;

    [DataField]
    public Entity<SolutionHolderComponent> Container = default!;

    [DataField, AutoNetworkedField]
    public List<ReagentData> Contents;

    /// <summary>
    ///     The name of this solution
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Name = SharedSolutionSystem.DefaultSolutionName;

    [DataField, AutoNetworkedField]
    public bool CanOverflow = true;

    public FixedPoint2 OverflowAmount => Volume - MaxVolume;

    /// <summary>
    ///     If reactions will be checked for when adding reagents to the container.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canReact"), AutoNetworkedField]
    public bool CanReact { get; set; } = true;

    /// <summary>
    ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Volume { get; set; }

    /// <summary>
    ///     Maximum volume this solution supports.
    /// </summary>
    /// <remarks>
    /// If overflow is enabled exceeding this volume will cause overflow events
    /// </remarks>
    [DataField("maxVol"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxVolume { get; set; } = FixedPoint2.Zero;

    public float FillFraction => MaxVolume == 0 ? 1 : Volume.Float() / MaxVolume.Float();

    /// <summary>
    ///     Volume needed to fill this container.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 AvailableVolume => MaxVolume - Volume;

    //TODO: just use temperature component dear god
    /// <summary>
    ///     The temperature of the reagents in the solution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Temperature;

    [DataField, AutoNetworkedField]
    public float ThermalEnergy;

    /// <summary>
    ///     The total heat capacity of all reagents in the solution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatCapacity;

    [DataField, AutoNetworkedField]
    public int PrimaryReagentIndex = -1;

    [DataField, AutoNetworkedField]
    public int ReagentVariantCount = 0;

    [ViewVariables]
    public int ReagentAndVariantCount => ReagentVariantCount + Contents.Count;


    [DataDefinition, Serializable, NetSerializable]
    public partial struct ReagentData : IEquatable<ReagentData>//This is a struct so that we don't allocate. Allocations are paying taxes. Nobody wants to pay taxes.
    {
        [DataField(required:true)]
        public string ReagentId = "";

        [NonSerialized]
        public Entity<ReagentDefinitionComponent> ReagentEnt;

        [DataField(required:true)]
        public FixedPoint2 Quantity;

        [DataField]
        public FixedPoint2 TotalQuantity;

        [ViewVariables]
        public int VariantCount => Variants?.Count ?? 0;

        [DataField]
        public List<VariantData>? Variants = null;

        [DataField]
        public int Index = -1;

        [NonSerialized]
        public bool IsValid = false;

        public ReagentData(Entity<ReagentDefinitionComponent> reagentEnt, FixedPoint2 quantity, int index)
        {
            Quantity = quantity;
            TotalQuantity = quantity;
            ReagentEnt = reagentEnt;
            Index = index;
            ReagentId = reagentEnt.Comp.Id;
            IsValid = true;
        }

        public bool Equals(ReagentData other)
        {
            return other.GetHashCode() == GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return ReagentId.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ReagentId}:{TotalQuantity}";
        }

        public static implicit operator ReagentQuantity(ReagentData d) =>
            new (d.ReagentEnt, d.Quantity, null);

        public static implicit operator ReagentDef(ReagentData d) =>
            new (d.ReagentEnt, null);
    }

    [DataDefinition, Serializable, NetSerializable]
    public partial struct VariantData
    {
        [DataField]
        public ReagentVariant Variant;
        [DataField]
        public FixedPoint2 Quantity;

        [DataField]
        public int ParentIndex = -1;

        [DataField(readOnly:true)]
        public bool IsValid = false;

        public VariantData(ReagentVariant variant, FixedPoint2 quantity, int parentReagentIndex)
        {
            Variant = variant;
            Quantity = quantity;
            ParentIndex = parentReagentIndex;
            IsValid = true;
        }
    };
}
