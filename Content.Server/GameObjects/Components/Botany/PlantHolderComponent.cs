using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.GameObjects.Components.Botany
{
    public enum Substrate
    {
        Empty,
        Sand,
        Rockwool
    }

    class PlantHolderComponent : Component, IAttackBy
    {
        public override string Name => "PlantHolder";

        [ViewVariables(VVAccess.ReadWrite)]
        private PlantComponent _heldPlant = null;
        public PlantComponent HeldPlant
        {
            get => _heldPlant;
            set
            {
                if (_heldPlant != null && value != null)
                {
                    throw new NotImplementedException();
                }

                _heldPlant = value;

                if (_heldPlant != null)
                {
                    _heldPlant.Holder = this;
                    _heldPlant.Owner.Transform.GridPosition = Owner.Transform.GridPosition.Offset(new Vector2(0, plantYOffset));
                    // todo: handle transform bullshit correctly
                    //_heldPlant.Owner.Transform.AttachParent(this.Owner);
                    //_heldPlant.Owner.Transform.LocalPosition = new Vector2(0,0);
                }
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Substrate HeldSubstrate;

        [ViewVariables(VVAccess.ReadWrite)]
        public float plantYOffset;

        private SpriteSpecifier emptySprite;
        private SpriteSpecifier sandSprite;
        private SpriteSpecifier rockwoolSprite;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref plantYOffset, "plantYOffset", 0.0f);
            serializer.DataField(ref HeldSubstrate, "heldSubstrate", Substrate.Empty);
            serializer.DataField(ref emptySprite, "emptySprite", null);
            serializer.DataField(ref sandSprite, "sandSprite", null);
            serializer.DataField(ref rockwoolSprite, "rockwoolSprite", null);
            //todo: serialize _heldPlant
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (HeldPlant != null)
            {
                HeldPlant.Holder = null;
            }
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (eventArgs.AttackWith.TryGetComponent(out PlantSeedComponent seedComponent))
            {
                if (HeldPlant != null)
                {
                    Owner.PopupMessage(eventArgs.User, "Can't plant this seed: there is already a plant there.");
                    return false;
                }
                else if (HeldSubstrate == Substrate.Empty)
                {
                    Owner.PopupMessage(eventArgs.User, "Can't plant this seed: there is no substrate to plant into.");
                    return false;
                }
                seedComponent.PlantIntoHolder(this);
                return true;
            }
            else if (eventArgs.AttackWith.TryGetComponent(out ShovelComponent shovel))
            {
                if (HeldPlant != null)
                {
                    Owner.PopupMessage(eventArgs.User, "Can't use this shovel: plant blocking the way.");
                    return false;
                }
                else if (HeldSubstrate == Substrate.Empty)
                {
                    Owner.PopupMessage(eventArgs.User, "Can't use this shovel: no substrate to shovel out.");
                    return false;
                }
                else
                {
                    HeldSubstrate = Substrate.Empty;
                    Owner.GetComponent<SpriteComponent>().LayerSetSprite(0, emptySprite);
                    return true;
                }
            }
            // Todo: consider demanding reagents rather than stacks?
            else if (eventArgs.AttackWith.TryGetComponent(out StackComponent stack))
            {
                if (HeldSubstrate != Substrate.Empty)
                {
                    Owner.PopupMessage(eventArgs.User, "Can't fill with this substrate: there is already a substrate in this.");
                    return false;
                }
                else if (HeldPlant != null)
                {
                    // This shouldn't be happening in the first place
                    Owner.PopupMessage(eventArgs.User, "Can't fill with this substrate: the plant is blocking that.");
                    return false;
                }
                else
                {
                    if ((StackType)stack.StackType == StackType.Rockwool && stack.Use(10))
                    {
                        HeldSubstrate = Substrate.Rockwool;
                        Owner.GetComponent<SpriteComponent>().LayerSetSprite(0, rockwoolSprite);
                        return true;
                    }
                    if ((StackType)stack.StackType == StackType.Sand && stack.Use(10))
                    {
                        HeldSubstrate = Substrate.Sand;
                        Owner.GetComponent<SpriteComponent>().LayerSetSprite(0, sandSprite);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
