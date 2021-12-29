using Content.Server.Light.EntitySystems;
using Content.Server.PowerCell.Components;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Light.Component;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a powered handheld light source which can be toggled on and off.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(HandheldLightSystem))]
    public sealed class HandheldLightComponent : SharedHandheldLightComponent
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("wattage")] public float Wattage { get; set; } = 3f;
        [ViewVariables] public PowerCellSlotComponent CellSlot = default!;
        public PowerCellComponent? Cell => CellSlot.Cell;

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated { get; set; }

        [ViewVariables] protected override bool HasCell => CellSlot.HasCell;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOnSound")] public SoundSpecifier TurnOnSound = new SoundPathSpecifier("/Audio/Items/flashlight_on.ogg");
        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOnFailSound")] public SoundSpecifier TurnOnFailSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");
        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOffSound")] public SoundSpecifier TurnOffSound = new SoundPathSpecifier("/Audio/Items/flashlight_off.ogg");

        /// <summary>
        ///     Client-side ItemStatus level
        /// </summary>
        public byte? LastLevel;
    }

    [UsedImplicitly]
    [DataDefinition]
    public class ToggleLightAction : IToggleItemAction
    {
        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<HandheldLightComponent?>(args.Item, out var lightComponent)) return false;
            if (lightComponent.Activated == args.ToggledOn) return false;
            return EntitySystem.Get<HandheldLightSystem>().ToggleStatus(args.Performer, lightComponent);
        }
    }
}
