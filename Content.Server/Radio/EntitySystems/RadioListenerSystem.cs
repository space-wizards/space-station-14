using System.Collections.Generic;
using Content.Server.Radio.Components;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Radio.EntitySystems
{
    [UsedImplicitly]
    public class RadioListenerSystem : EntitySystem
    {
        [Dependency] private readonly RadioSystem _radioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RadioListenerComponent, RadioListenerToggledEvent>(OnListenerToggled);
            SubscribeLocalEvent<RadioListenerComponent, ExaminedEvent>(OnListenerExamined);
        }

        private void OnListenerToggled(EntityUid uid, RadioListenerComponent component, RadioListenerToggledEvent args)
        {
            if(args.Value)
        }
    }

    public class RadioListenerToggledEvent : EntityEventArgs
    {
    }
}
