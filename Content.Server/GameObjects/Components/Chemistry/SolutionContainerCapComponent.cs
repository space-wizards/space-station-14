#nullable enable
using Content.Server.GameObjects.Components.Fluids;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    ///     Allow to open and close solution container cap.
    ///     Users can't drink from closed containers and need to open them first.
    ///     Thrown container with closed cap won't spill on floor.
    /// </summary>
    [RegisterComponent]
    public class SolutionContainerCapComponent : Component, IUse, ILand, IExamine
    {
        public override string Name => "ContainerCap";

        [DataField("isOpen")] private bool _defaultToOpened = false;
        [DataField("canBeClosed")] private bool _canBeClosed = true;
        [DataField("openSounds")] private string _soundCollection = "canOpenSounds";
        [DataField("pressurized")] private bool _pressurized = false;
        [DataField("burstSound")] private string _burstSound = "/Audio/Effects/flash_bang.ogg";

        [ComponentDependency] private readonly AppearanceComponent? _appearance;
        [ComponentDependency] private readonly SolutionContainerComponent? _solutionContainer;

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private bool _opened;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Opened
        {
            get => _opened;
            set
            {
                if (_opened == value)
                {
                    return;
                }

                _opened = value;
                OpenedChanged();
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            Opened = _defaultToOpened;
        }

        private void OpenedChanged()
        {
            // set correct caps for container
            if (_solutionContainer != null)
            {
                if (_opened)
                {
                    _solutionContainer.Capabilities |= SolutionContainerCaps.Refillable | SolutionContainerCaps.Drainable;
                }
                else
                {
                    _solutionContainer.Capabilities &= ~(SolutionContainerCaps.Refillable | SolutionContainerCaps.Drainable);
                }
            }

            // update appearance
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange)
                return;

            if (!Opened)
            {
                var closedText = Loc.GetString("comp-solutioncontainercap-examine-closed");
                message.AddMarkup(closedText);
            }            
        }

        bool IUse.UseEntity(UseEntityEventArgs args)
        {
            // check if container can be opened
            if (!_opened)
            {
                //Do the opening stuff like playing the sounds.
                var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(_soundCollection);
                var file = _random.Pick(soundCollection.PickFiles);
                EntitySystem.Get<AudioSystem>().Play(Filter.Broadcast(), file, args.User, AudioParams.Default);

                Opened = true;
                return true;
            }

            return false;
        }

        void ILand.Land(LandEventArgs eventArgs)
        {
            // thrown item will spill all content on the floor
            if (Opened)
            {
                SpillSolution();
            }
            // there is small chance that thrown closed pressurized container will explode
            // imagine something like soda can thrown into wall
            else if (_pressurized && !Opened &&  _random.Prob(0.25f))
            {
                Opened = true;
                SpillSolution();

                EntitySystem.Get<AudioSystem>().Play(Filter.Broadcast(), _burstSound,
                    Owner, AudioParams.Default.WithVolume(-4));
            }
        }

        private void SpillSolution()
        {
            if (!Owner.TryGetComponent(out ISolutionInteractionsComponent? interactions))
                return;

            if (!interactions.CanDrain)
                return;

            var solution = interactions.Drain(interactions.DrainAvailable);
            solution.SpillAt(Owner, "PuddleSmear");
        }
    }
}
