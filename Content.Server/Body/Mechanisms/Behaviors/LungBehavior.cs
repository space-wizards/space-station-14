#nullable enable
using System;
using Content.Server.Body.Network;
using Content.Server.GameObjects.Components.Body.Respiratory;
using JetBrains.Annotations;

namespace Content.Server.Body.Mechanisms.Behaviors
{
    [UsedImplicitly]
    public class LungBehavior : MechanismBehavior
    {
        protected override Type? Network => typeof(RespiratoryNetwork);

        public override void PreMetabolism(float frameTime)
        {
            base.PreMetabolism(frameTime);

            if (Mechanism.Body == null ||
                !Mechanism.Body.Owner.TryGetComponent(out LungComponent? lung))
            {
                return;
            }

            lung.Update(frameTime);
        }
    }
}
