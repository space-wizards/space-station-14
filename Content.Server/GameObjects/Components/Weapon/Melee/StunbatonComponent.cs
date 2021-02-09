#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Power;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class StunbatonComponent : MeleeWeaponComponent, IUse, IExamine, IInteractUsing, IThrowCollide
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "Stunbaton";

        private bool _activated = false;

        [ViewVariables] private PowerCellSlotComponent _cellSlot = default!;
        private PowerCellComponent? Cell => _cellSlot.Cell;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeChanceNoSlowdown = 0.35f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeChanceWithSlowdown = 0.85f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeTime = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _slowdownTime = 5f;

        [ViewVariables(VVAccess.ReadWrite)] public float EnergyPerUse { get; set; } = 50;

        [ViewVariables]
        public bool Activated => _activated;

        public override void Initialize()
        {
            base.Initialize();
            _cellSlot = Owner.EnsureComponent<PowerCellSlotComponent>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _paralyzeChanceNoSlowdown, "paralyzeChanceNoSlowdown", 0.35f);
            serializer.DataField(ref _paralyzeChanceWithSlowdown, "paralyzeChanceWithSlowdown", 0.85f);
            serializer.DataField(ref _paralyzeTime, "paralyzeTime", 10f);
            serializer.DataField(ref _slowdownTime, "slowdownTime", 5f);
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerCellChangedMessage m:
                    if (component is PowerCellSlotComponent slotComponent && slotComponent == _cellSlot)
                    {
                        if (m.Ejected)
                        {
                            TurnOff();
                        }
                    }
                    break;
            }
        }

        protected override bool OnHitEntities(IReadOnlyList<IEntity> entities, AttackEventArgs eventArgs)
        {
            if (!Activated || entities.Count == 0 || Cell == null)
                return true;

            if (!Cell.TryUseCharge(EnergyPerUse))
                return true;

            EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Weapons/egloves.ogg", Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out StunnableComponent? stunnable)) continue;

                if(!stunnable.SlowedDown)
                    if(_robustRandom.Prob(_paralyzeChanceNoSlowdown))
                        stunnable.Paralyze(_paralyzeTime);
                    else
                        stunnable.Slowdown(_slowdownTime);
                else
                    if(_robustRandom.Prob(_paralyzeChanceWithSlowdown))
                        stunnable.Paralyze(_paralyzeTime);
                    else
                        stunnable.Slowdown(_slowdownTime);
            }

            if (!(Cell.CurrentCharge < EnergyPerUse)) return true;

            EntitySystem.Get<AudioSystem>().PlayAtCoords(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
            TurnOff();

            return true;
        }

        private bool ToggleStatus(IEntity user)
        {
            if (!ActionBlockerSystem.CanUse(user)) return false;
            if (Activated)
            {
                TurnOff();
            }
            else
            {
                TurnOn(user);
            }

            return true;
        }

        private void TurnOff()
        {
            if (!_activated)
            {
                return;
            }

            var sprite = Owner.GetComponent<SpriteComponent>();
            var item = Owner.GetComponent<ItemComponent>();

            EntitySystem.Get<AudioSystem>().PlayAtCoords(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "off";
            sprite.LayerSetState(0, "stunbaton_off");
            _activated = false;
        }

        private void TurnOn(IEntity user)
        {
            if (_activated)
            {
                return;
            }

            var sprite = Owner.GetComponent<SpriteComponent>();
            var item = Owner.GetComponent<ItemComponent>();

            if (Cell == null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Machines/button.ogg", Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

                Owner.PopupMessage(user, Loc.GetString("Cell missing..."));
                return;
            }

            if (Cell.CurrentCharge < EnergyPerUse)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Machines/button.ogg", Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));
                Owner.PopupMessage(user, Loc.GetString("Dead cell..."));
                return;
            }

            EntitySystem.Get<AudioSystem>().PlayAtCoords(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "stunbaton_on");
            _activated = true;
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            ToggleStatus(eventArgs.User);

            return true;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User)) return false;
            if (!_cellSlot.InsertCell(eventArgs.Using)) return false;
            Dirty();
            return true;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (Activated)
            {
                message.AddMarkup(Loc.GetString("The light is currently [color=darkgreen]on[/color]."));
            }
        }

        void IThrowCollide.DoHit(ThrowCollideEventArgs eventArgs)
        {
            if (!Activated || Cell == null || !Cell.TryUseCharge(EnergyPerUse) || !eventArgs.Target.TryGetComponent(out StunnableComponent? stunnable))
                return;

            EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Weapons/egloves.ogg", Owner.Transform.Coordinates, AudioHelpers.WithVariation(0.25f));

            stunnable.Paralyze(_paralyzeTime);
        }
    }
}
