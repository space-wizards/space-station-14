using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class CreamPiedComponent : SharedCreamPiedComponent, IReagentReaction, IThrowCollide
    {
        private bool _creamPied;

        [ViewVariables]
        public bool CreamPied
        {
            get => _creamPied;
            private set
            {
                _creamPied = value;
                if (Owner.TryGetComponent(out AppearanceComponent appearance))
                {
                    appearance.SetData(CreamPiedVisuals.Creamed, CreamPied);
                }
            }
        }

        public ReagentUnit ReagentReactTouch(ReagentPrototype reagent, ReagentUnit volume)
        {
            switch (reagent.ID)
            {
                case "chem.SpaceCleaner":
                case "chem.H2O":
                    if (CreamPied)
                        CreamPied = false;
                    break;
            }

            return ReagentUnit.Zero;
        }

        public void ThrowCollide(ThrowCollideEventArgs eventArgs)
        {
            if (eventArgs.Target != Owner || !eventArgs.Thrown.TryGetComponent(out CreamPieComponent creamPie) || CreamPied) return;

            CreamPied = true;
        }
    }
}
