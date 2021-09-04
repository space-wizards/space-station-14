using Content.Server.CombatMode;
using Content.Server.Interaction;
using Content.Server.Notification;
using Content.Server.Power.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Notification.Managers;
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
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;
using Content.Server.Xenobiology;
using Content.Server.Storage.Components;

namespace Content.Server.Xenobiology
{
    [RegisterComponent]
    public class SpecimenContainmentComponent : SharedXenoTubeComponent
    {
        public override string Name => "SpecimenContainment";

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

        [Verb]
        public sealed class DebugCreateSpecimenVerb : Verb<SpecimenContainmentComponent>
        {
            protected override void GetData(IEntity user, SpecimenContainmentComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }
                data.Text = Loc.GetString("debug-specimen-create");
                data.Visibility = !component.IsOccupied ? VerbVisibility.Visible : VerbVisibility.Invisible;

            }

            protected override void Activate(IEntity user, SpecimenContainmentComponent component)
            {
                component.DebugCreateSpecimen();
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
            if (containedEntity.TryGetComponent<SpecimenDietComponent>(out SpecimenDietComponent? DietComp))
            {
                if (DietComp.GrowthState < 5) return;
            }
            TubeContainer.Remove(containedEntity);
            
            UpdateAppearance();
        }

        public void DebugCreateSpecimen()
        {
            IEntity debugspecimen = Owner.EntityManager.SpawnEntity("specimen_basic", Owner.Transform.Coordinates);
            TubeContainer.Insert(debugspecimen);
            UpdateAppearance();
        }

        //Communicates with the visualiser through a Shared component
        public void UpdateAppearance() 
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearancecomp))
            {
                appearancecomp.SetData(SharedXenoTubeComponent.XenoTubeStatus.Powered, Powered);
                appearancecomp.SetData(SharedXenoTubeComponent.XenoTubeStatus.Occupied, TubeContainer.ContainedEntity != null);
            }
        }
    }
}
