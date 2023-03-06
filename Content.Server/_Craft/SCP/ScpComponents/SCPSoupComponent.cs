using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Content.Shared.SCP.ConcreteSlab;
using Content.Shared.Actions;

namespace Content.Server.SCP.Soap
{
    [RegisterComponent]
    //[Access(typeof(SCP173System))]
    public sealed class SCPSoapComponent : Component
    {
        [DataField("slipActionRange")]
        public float SlipActionRange = 1;

        [DataField("slipActionForce")]
        public float SlipActionForce = 15;

        [DataField("slipActionStun")]
        public float SlipActionStun = 4;

        [DataField("slipActionSound")]
        public SoundSpecifier SlipActionSound = new SoundPathSpecifier("/Audio/Effects/slip.ogg");

        [DataField("slipAction")]
        public InstantAction SlipAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(32),
            Icon = new SpriteSpecifier.Texture(new("Interface/Actions/malfunction.png")),
            ItemIconStyle = ItemActionIconStyle.NoItem,
            DisplayName = "scp-soap-trip",
            Description = "scp-soap-trip-desc",
            Priority = -1,
            Event = new SlipActionEvent(),
        };

		// Я пока еще подумаю
        //[DataField("cleanAction")]
        //public InstantAction CleanAction = new()
        //{
        //    Enabled = false,
        //    UseDelay = TimeSpan.FromSeconds(90),
        //    Icon = new SpriteSpecifier.Texture(new("Interface/Actions/malfunction.png")),
        //    ItemIconStyle = ItemActionIconStyle.NoItem,
        //    DisplayName = "scp-173-blind",
        //    Description = "scp-173-blind-desc",
        //    Priority = -1,
        //    Event = new CleanActionEvent(),
        //};
    }
}
