using Content.Server.Interaction.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Jobs
{
    [UsedImplicitly]
    public sealed class AddComponentSpecial : JobSpecial
    {
        // TODO: Type serializer that ensures the component exists.
        [DataField("component", required:true)]
        public string Component { get; } = string.Empty;

        public override void AfterEquip(IEntity mob)
        {
            // Yes, this will throw if your component is invalid.
            var component = (Component)IoCManager.Resolve<IComponentFactory>().GetComponent(Component);
            component.Owner = mob;

            IoCManager.Resolve<IEntityManager>().AddComponent(mob, component);
        }
    }
}
