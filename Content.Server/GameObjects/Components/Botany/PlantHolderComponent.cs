using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems;
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

        private PlantComponent _heldPlant;
        public PlantComponent HeldPlant
        {
            get => _heldPlant;
            set
            {
                _heldPlant.Holder = this;
                // todo: handle transform bullshit correctly
                _heldPlant.Owner.Transform.LocalPosition = Owner.Transform.LocalPosition;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Substrate HeldSubstrate = Substrate.Empty;

        private SpriteSpecifier emptySprite;
        private SpriteSpecifier sandSprite;
        private SpriteSpecifier rockwoolSprite;



        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref emptySprite, "emptySprite", null);
            serializer.DataField(ref sandSprite, "sandSprite", null);
            serializer.DataField(ref rockwoolSprite, "rockwoolSprite", null);
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            if (eventArgs.AttackWith.TryGetComponent(out PlantSeedComponent seedComponent))
            {
                // plant the seed
                return true;
            }
            else if (eventArgs.AttackWith.TryGetComponent(out ShovelComponent shovel))
            {
                // you shovel out the substrate
                HeldSubstrate = Substrate.Empty;
                Owner.GetComponent<SpriteComponent>().LayerSetSprite(0, emptySprite);
                return true;
            }
            // Todo: consider demanding reagents rather than stacks?
            else if (eventArgs.AttackWith.TryGetComponent(out StackComponent stack))
            {
                if (HeldSubstrate == Substrate.Empty)
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
