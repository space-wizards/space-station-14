using Content.Server.Access.Components;
using Content.Shared.Access.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Access.Systems
{
    [UsedImplicitly]
    public sealed class IdCardConsoleSystem : SharedIdCardConsoleSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            // one day, maybe bound user interfaces can be shared too.
            SubscribeLocalEvent<IdCardConsoleComponent, ComponentStartup>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<IdCardConsoleComponent, EntInsertedIntoContainerMessage>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<IdCardConsoleComponent, EntRemovedFromContainerMessage>((_, comp, _) => comp.UpdateUserInterface());
        }
    }
}
