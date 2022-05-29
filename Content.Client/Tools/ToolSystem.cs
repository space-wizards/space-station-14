using Content.Client.Tools.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.Tools
{
    public sealed class ToolSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WelderComponent, ComponentHandleState>(OnWelderHandleState);
        }

        private void OnWelderHandleState(EntityUid uid, WelderComponent welder, ref ComponentHandleState args)
        {
            if (args.Current is not WelderComponentState state)
                return;

            welder.FuelCapacity = state.FuelCapacity;
            welder.Fuel = state.Fuel;
            welder.Lit = state.Lit;
            welder.UiUpdateNeeded = true;
        }
    }
}
