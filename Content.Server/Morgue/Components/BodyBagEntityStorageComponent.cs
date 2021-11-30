using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Paper;
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Morgue;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class BodyBagEntityStorageComponent : EntityStorageComponent
    {
        public override string Name => "BodyBagEntityStorage";

        protected override bool AddToContents(IEntity entity)
        {
            if (entity.HasComponent<SharedBodyComponent>() && !EntitySystem.Get<StandingStateSystem>().IsDown(entity.Uid)) return false;
            return base.AddToContents(entity);
        }
    }
}
