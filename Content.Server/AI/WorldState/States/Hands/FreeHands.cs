using System.Linq;
using Content.Server.Hands.Components;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Hands
{
    [UsedImplicitly]
    public sealed class FreeHands : StateData<List<string>>
    {
        public override string Name => "FreeHands";

        public override List<string> GetValue()
        {
            var result = new List<string>();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out HandsComponent? handsComponent))
            {
                return new List<string>();
            }

            return handsComponent.GetFreeHandNames().ToList();
        }
    }
}
