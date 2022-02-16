using Content.Shared.Chemistry.Components;
using Content.Shared.Kitchen.Components;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Kitchen.Components
{
    /// <summary>
    /// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
    /// think of grinding as a utility to break an object down into its reagents. Think of juicing as
    /// converting something into its single juice form. E.g, grind an apple and get the nutriment and sugar
    /// it contained, juice an apple and get "apple juice".
    /// </summary>
    [RegisterComponent]
    public sealed class ReagentGrinderComponent : SharedReagentGrinderComponent
    {
        [ViewVariables] public ContainerSlot BeakerContainer = default!;

        /// <summary>
        /// Can be null since we won't always have a beaker in the grinder.
        /// </summary>
        [ViewVariables] public Solution? HeldBeaker = default!;

        /// <summary>
        /// Contains the things that are going to be ground or juiced.
        /// </summary>
        [ViewVariables] public Container Chamber = default!;

        /// <summary>
        /// Is the machine actively doing something and can't be used right now?
        /// </summary>
        public bool Busy;

        //YAML serialization vars
        [ViewVariables(VVAccess.ReadWrite)] [DataField("chamberCapacity")] public int StorageCap = 16;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("workTime")] public int WorkTime = 3500; //3.5 seconds, completely arbitrary for now.
        [DataField("clickSound")] public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
        [DataField("grindSound")] public SoundSpecifier GrindSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/blender.ogg");
        [DataField("juiceSound")] public SoundSpecifier JuiceSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");
    }
}
