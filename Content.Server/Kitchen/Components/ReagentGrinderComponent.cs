using Content.Shared.Kitchen;
using Content.Server.Kitchen.EntitySystems;
using Robust.Shared.Audio;

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
        //YAML serialization vars
        [DataField("storageMaxEntities"), ViewVariables(VVAccess.ReadWrite)]
        public int StorageMaxEntities = 16;

        [DataField("workTime"), ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan WorkTime = TimeSpan.FromSeconds(3.5); // Roughly matches the grind/juice sounds.

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [DataField("grindSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier GrindSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

        [DataField("juiceSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier JuiceSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/juicer.ogg");
    }

    [Access(typeof(ReagentGrinderSystem)), RegisterComponent]
    public sealed class ActiveReagentGrinderComponent : Component
    {
        /// <summary>
        /// Remaining time until the grinder finishes grinding/juicing.
        /// </summary>
        [ViewVariables]
        public float WorkTimer;

        [ViewVariables]
        public GrinderProgram Program;
    }
}
