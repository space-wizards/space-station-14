    #nullable enable
    using Content.Shared.GameObjects;
    using Robust.Shared.GameObjects;
    using Robust.Shared.Serialization.Manager.Attributes;
    using Robust.Shared.ViewVariables;

    namespace Content.Server.GameObjects.Components.Engineering
    {
        [RegisterComponent]
        public class DisassembleOnActivateComponent : Component
    {
        public override string Name => "DisassembleOnActivate";
        public override uint? NetID => ContentNetIDs.DISASSEMBLE_ON_ACTIVATE;

        [ViewVariables]
        [DataField("prototype")]
        public string? Prototype { get; private set; }

        [ViewVariables]
        [DataField("doAfter")]
        public float DoAfterTime = 0;
    }
}
