using Content.Server.Notification;
using Content.Server.Stunnable.Components;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Nutrition.Components;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.Nutrition.Components
{
    [RegisterComponent]
    public class CreamPiedComponent : SharedCreamPiedComponent, IThrowCollide
    {
        private bool _creamPied;

        [ViewVariables]
        public bool CreamPied
        {
            get => _creamPied;
            private set
            {
                if (value == _creamPied) return;

                _creamPied = value;
                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(CreamPiedVisuals.Creamed, CreamPied);
                }
            }
        }

        public void Wash()
        {
            if(CreamPied)
                CreamPied = false;
        }

        void IThrowCollide.HitBy(ThrowCollideEventArgs eventArgs)
        {
            if (eventArgs.Thrown.Deleted || !eventArgs.Thrown.TryGetComponent(out CreamPieComponent? creamPie)) return;

            CreamPied = true;
            Owner.PopupMessage(Loc.GetString("cream-pied-component-on-hit-by-message",("thrower", eventArgs.Thrown)));
            Owner.PopupMessageOtherClients(Loc.GetString("cream-pied-component-on-hit-by-message-others", ("owner", Owner),("thrower", eventArgs.Thrown)));

            if (Owner.TryGetComponent(out StunnableComponent? stun))
            {
                stun.Paralyze(creamPie.ParalyzeTime);
            }
        }
    }
}
