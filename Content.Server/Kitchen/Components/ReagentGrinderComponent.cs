using Content.Shared.Kitchen.Components;
using Content.Server.Kitchen.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Kitchen.Components
{
    /// <summary>
    /// The combo reagent grinder/juicer. The reason why grinding and juicing are seperate is simple,
    /// think of grinding as a utility to break an object down into its reagents. Think of juicing as
    /// converting something into its single juice form. E.g, grind an apple and get the nutriment and sugar
    /// it contained, juice an apple and get "apple juice".
    /// </summary>
    [Access(typeof(ReagentGrinderSystem)), RegisterComponent]
    public sealed class ReagentGrinderComponent : Component
    {
        /// <summary>
        /// Is the machine actively doing something and can't be used right now?
        /// </summary>
        public bool Busy;

        //YAML serialization vars
        [DataField("chamberCapacity"), ViewVariables(VVAccess.ReadWrite)]
        public int StorageCap = 16;

        [DataField("workTime"), ViewVariables(VVAccess.ReadWrite)]
        public int WorkTime = 3500; //3.5 seconds, completely arbitrary for now.

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [DataField("grindSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier GrindSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

        [DataField("juiceSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier JuiceSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");
    }
}
