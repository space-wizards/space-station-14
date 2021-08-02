using Content.Server.CombatMode;
using Content.Server.Interaction;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Verbs;
using Content.Shared.Xenobiology;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Numerics;
using YamlDotNet.Core.Tokens;


namespace Content.Server.Xenobiology
{
    [RegisterComponent]
    public class SpecimenContainmentComponent : SharedXenoTubeComponent, IDestroyAct
    {
        public override string Name => "SpecimenContainmentComponent";

        private readonly Vector2 _ejectOffset = new(0f, 0f);

        [ViewVariables] public ContainerSlot TubeContainer = default!;

        public bool IsOccupied => TubeContainer.ContainedEntity != null;

        public bool Powered; //Set by the XenobiologyTubeSystem, OnPowerChanged() event to update in real-time

        protected override void Initialize()
        {
            base.Initialize();
            TubeContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "SpecimenContainer");
            UpdateAppearance();
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateAppearance();
        }

        [Verb]
        public sealed class EnterVerb : Verb<SpecimenContainmentComponent>
        {
            protected override void GetData(IEntity user, SpecimenContainmentComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("enter-verb-get-data-text");
                data.Visibility = component.IsOccupied ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, SpecimenContainmentComponent component)
            {
                component.InsertBody(user);
            }
        }

        [Verb]
        public sealed class EjectVerb : Verb<SpecimenContainmentComponent>
        {
            protected override void GetData(IEntity user, SpecimenContainmentComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("eject-verb-get-data-text");
                data.Visibility = component.IsOccupied ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, SpecimenContainmentComponent component)
            {
                component.EjectBody();
            }
        }

        public void InsertBody(IEntity user)
        {
            TubeContainer.Insert(user);
            UpdateAppearance();
        }

        public void EjectBody()
        {
            var containedEntity = TubeContainer.ContainedEntity;
            if (containedEntity == null) return;
            TubeContainer.Remove(containedEntity);
            UpdateAppearance();
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            EjectBody();
        }

        public void UpdateAppearance() //Communicates with the visualiser through a Shared component
        { 
            if (Owner.TryGetComponent(out AppearanceComponent? appearancecomp))
            {
                appearancecomp.SetData(SharedXenoTubeComponent.XenoTubeStatus.Powered, Powered);
                appearancecomp.SetData(SharedXenoTubeComponent.XenoTubeStatus.Occupied, TubeContainer.ContainedEntity != null);
            }
        }

    }
}
