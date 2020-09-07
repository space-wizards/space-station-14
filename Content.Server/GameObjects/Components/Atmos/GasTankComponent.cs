using System;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasTankComponent : ItemComponent, IExamine, IGasMixtureHolder
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private const float MinimumThreshold = 16f;
        private const float DefaultOutputRatio = 0.01f;
        public override string Name => "GasTank";
        public override uint? NetID => ContentNetIDs.GAS_TANK;

        [ViewVariables] public GasMixture Air { get; set; }
        [ViewVariables] public bool IsOpen { get; set; }
        [ViewVariables] public float OutputRatio { get; set; }

        [ViewVariables] public IConnectableToGasTank Connectable { get; set; }

        [ViewVariables] public bool IsConnected => Connectable != null;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("air", new GasMixture(), x => Air = x, () => Air);
            serializer.DataReadWriteFunction("isOpen", false, x => IsOpen = x, () => IsOpen);
            serializer.DataReadWriteFunction("outputRatio", DefaultOutputRatio, x => OutputRatio = x, () => OutputRatio);

        }


        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("Pressure: [color={0}]{1}[/color] kPa.",
                Air.Pressure < MinimumThreshold ? "darkorange" : "orange", Math.Round(Air.Pressure)));
            message.AddMarkup(Loc.GetString("\nValve: [color={0}]{1}[/color]", IsOpen ? "green" : "red", IsOpen? "Open" : "Closed"));
            if (IsConnected)
            {
                message.AddMarkup(Loc.GetString("\nConnected to external component"));
            }
        }

        public void Update()
        {
            if (!IsOpen || Air.TotalMoles == 0 || IsConnected) return;
            var gas = Air.RemoveRatio(OutputRatio);
            var tile = Owner.Transform.Coordinates.GetTileAtmosphere(_entityManager);
            if (tile?.Air == null) return;
            tile.AssumeAir(gas);
        }

        public override void Unequipped(UnequippedEventArgs eventArgs)
        {
            if (!IsConnected) return;
            Connectable.Disconnect();
        }


        [Verb]
        private sealed class ValveStateVerb : Verb<GasTankComponent>
        {
            protected override void GetData(IEntity user, GasTankComponent component, VerbData data)
            {
                data.Text = Loc.GetString(component.IsOpen
                    ? "Valve: Open"
                    : "Valve: Closed");
                data.Visibility = CheckVisibility(user, component.Owner);
            }

            protected override void Activate(IEntity user, GasTankComponent component)
            {
                if (CheckVisibility(user, component.Owner) == VerbVisibility.Invisible) return;
                component.IsOpen = !component.IsOpen;
            }

            private static VerbVisibility CheckVisibility(IEntity user, IEntity tank)
            {
                if (ContainerHelpers.TryGetContainer(tank, out var container))
                {
                    return container.Owner == user
                        ? VerbVisibility.Visible
                        : VerbVisibility.Invisible;
                }

                return user.InRangeUnobstructed(tank)
                    ? VerbVisibility.Visible
                    : VerbVisibility.Invisible;
            }
        }

    }
}
