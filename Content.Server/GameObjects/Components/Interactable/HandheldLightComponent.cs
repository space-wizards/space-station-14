#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a powered handheld light source which can be toggled on and off.
    /// </summary>
    [RegisterComponent]
    internal sealed class HandheldLightComponent : SharedHandheldLightComponent, IUse, IExamine, IInteractUsing
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("wattage")] public float Wattage { get; set; } = 3f;
        [ViewVariables] private PowerCellSlotComponent _cellSlot = default!;
        private PowerCellComponent? Cell => _cellSlot.Cell;

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated { get; private set; }

        [ViewVariables] protected override bool HasCell => _cellSlot.HasCell;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOnSound")] public string? TurnOnSound = "/Audio/Items/flashlight_toggle.ogg";
        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOnFailSound")] public string? TurnOnFailSound = "/Audio/Machines/button.ogg";
        [ViewVariables(VVAccess.ReadWrite)] [DataField("turnOffSound")] public string? TurnOffSound = "/Audio/Items/flashlight_toggle.ogg";

        [ComponentDependency] private readonly ItemActionsComponent? _itemActions = null;

        /// <summary>
        ///     Client-side ItemStatus level
        /// </summary>
        private byte? _lastLevel;

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<PointLightComponent>();
            _cellSlot = Owner.EnsureComponent<PowerCellSlotComponent>();

            Dirty();
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Owner.EntityManager.EventBus.QueueEvent(EventSource.Local, new DeactivateHandheldLightMessage(this));
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User)) return false;
            if (!_cellSlot.InsertCell(eventArgs.Using)) return false;
            Dirty();
            return true;
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Activated)
            {
                message.AddMarkup(Loc.GetString("The light is currently [color=darkgreen]on[/color]."));
            }
            else
            {
                message.AddMarkup(Loc.GetString("The light is currently [color=darkred]off[/color]."));
            }
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return ToggleStatus(eventArgs.User);
        }

        /// <summary>
        ///     Illuminates the light if it is not active, extinguishes it if it is active.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        public bool ToggleStatus(IEntity user)
        {
            if (!ActionBlockerSystem.CanUse(user)) return false;
            return Activated ? TurnOff() : TurnOn(user);
        }

        private bool TurnOff(bool makeNoise = true)
        {
            if (!Activated)
            {
                return false;
            }

            SetState(false);
            Activated = false;
            UpdateLightAction();
            Owner.EntityManager.EventBus.QueueEvent(EventSource.Local, new DeactivateHandheldLightMessage(this));

            if (makeNoise)
            {
                if (TurnOffSound != null) EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOffSound, Owner);
            }

            return true;
        }

        private bool TurnOn(IEntity user)
        {
            if (Activated)
            {
                return false;
            }

            if (Cell == null)
            {
                if (TurnOnFailSound != null) EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOnFailSound, Owner);
                Owner.PopupMessage(user, Loc.GetString("Cell missing..."));
                UpdateLightAction();
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            // Simple enough.
            if (Wattage > Cell.CurrentCharge)
            {
                if (TurnOnFailSound != null) EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOnFailSound, Owner);
                Owner.PopupMessage(user, Loc.GetString("Dead cell..."));
                UpdateLightAction();
                return false;
            }

            Activated = true;
            UpdateLightAction();
            SetState(true);
            Owner.EntityManager.EventBus.QueueEvent(EventSource.Local, new ActivateHandheldLightMessage(this));

            if (TurnOnSound != null) EntitySystem.Get<AudioSystem>().PlayFromEntity(TurnOnSound, Owner);
            return true;
        }

        private void SetState(bool on)
        {
            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.LayerSetVisible(1, on);
            }

            if (Owner.TryGetComponent(out PointLightComponent? light))
            {
                light.Enabled = on;
            }

            if (Owner.TryGetComponent(out ClothingComponent? clothing))
            {
                clothing.ClothingEquippedPrefix = on ? "on" : "off";
            }

            if (Owner.TryGetComponent(out ItemComponent? item))
            {
                item.EquippedPrefix = on ? "on" : "off";
            }
        }

        private void UpdateLightAction()
        {
            _itemActions?.Toggle(ItemActionType.ToggleLight, Activated);
        }

        public void OnUpdate(float frameTime)
        {
            if (Cell == null)
            {
                TurnOff(false);
                return;
            }

            var appearanceComponent = Owner.GetComponent<AppearanceComponent>();

            if (Cell.MaxCharge - Cell.CurrentCharge < Cell.MaxCharge * 0.70)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.FullPower);
            }
            else if (Cell.MaxCharge - Cell.CurrentCharge < Cell.MaxCharge * 0.90)
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.LowPower);
            }
            else
            {
                appearanceComponent.SetData(HandheldLightVisuals.Power, HandheldLightPowerStates.Dying);
            }

            if (Activated && !Cell.TryUseCharge(Wattage * frameTime)) TurnOff(false);

            var level = GetLevel();

            if (level != _lastLevel)
            {
                _lastLevel = level;
                Dirty();
            }
        }

        // Curently every single flashlight has the same number of levels for status and that's all it uses the charge for
        // Thus we'll just check if the level changes.
        private byte? GetLevel()
        {
            if (Cell == null)
                return null;

            var currentCharge = Cell.CurrentCharge;

            if (MathHelper.CloseTo(currentCharge, 0) || Wattage > currentCharge)
                return 0;

            return (byte?) ContentHelpers.RoundToNearestLevels(currentCharge / Cell.MaxCharge * 255, 255, StatusLevels);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new HandheldLightComponentState(GetLevel());
        }

        [Verb]
        public sealed class ToggleLightVerb : Verb<HandheldLightComponent>
        {
            protected override void GetData(IEntity user, HandheldLightComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Toggle light");
            }

            protected override void Activate(IEntity user, HandheldLightComponent component)
            {
                component.ToggleStatus(user);
            }
        }
    }

    [UsedImplicitly]
    [DataDefinition]
    public class ToggleLightAction : IToggleItemAction
    {
        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            if (!args.Item.TryGetComponent<HandheldLightComponent>(out var lightComponent)) return false;
            if (lightComponent.Activated == args.ToggledOn) return false;
            return lightComponent.ToggleStatus(args.Performer);
        }
    }

    internal sealed class ActivateHandheldLightMessage : EntitySystemMessage
    {
        public HandheldLightComponent Component { get; }

        public ActivateHandheldLightMessage(HandheldLightComponent component)
        {
            Component = component;
        }
    }

    internal sealed class DeactivateHandheldLightMessage : EntitySystemMessage
    {
        public HandheldLightComponent Component { get; }

        public DeactivateHandheldLightMessage(HandheldLightComponent component)
        {
            Component = component;
        }
    }
}
