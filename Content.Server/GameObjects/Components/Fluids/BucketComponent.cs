#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Fluids
{
    /// <summary>
    /// Can a mop click on this entity and dump its fluids
    /// </summary>
    [RegisterComponent]
    public class BucketComponent : Component, IInteractUsing
    {
        public override string Name => "Bucket";

        private List<EntityUid> _currentlyUsing = new();

        public ReagentUnit MaxVolume
        {
            get => Owner.TryGetComponent(out SolutionContainerComponent? solution) ? solution.MaxVolume : ReagentUnit.Zero;
            set
            {
                if (Owner.TryGetComponent(out SolutionContainerComponent? solution))
                {
                    solution.MaxVolume = value;
                }
            }
        }

        public ReagentUnit CurrentVolume => Owner.TryGetComponent(out SolutionContainerComponent? solution)
            ? solution.CurrentVolume
            : ReagentUnit.Zero;

        [DataField("sound")]
        private string? _sound = "/Audio/Effects/Fluids/watersplash.ogg";

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<SolutionContainerComponent>();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? contents) ||
                _currentlyUsing.Contains(eventArgs.Using.Uid) ||
                !eventArgs.Using.TryGetComponent(out MopComponent? mopComponent) ||
                mopComponent.Mopping)
            {
                return false;
            }

            if (CurrentVolume <= 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Bucket is empty"));
                return false;
            }

            if (mopComponent.CurrentVolume == mopComponent.MaxVolume)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Mop is full"));
                return false;
            }

            _currentlyUsing.Add(eventArgs.Using.Uid);

            // IMO let em move while doing it.
            var doAfterArgs = new DoAfterEventArgs(eventArgs.User, 1.0f, target: eventArgs.Target)
            {
                BreakOnStun = true,
                BreakOnDamage = true,
            };
            var result = await EntitySystem.Get<DoAfterSystem>().DoAfter(doAfterArgs);

            _currentlyUsing.Remove(eventArgs.Using.Uid);

            if (result == DoAfterStatus.Cancelled ||
                Owner.Deleted ||
                mopComponent.Deleted ||
                CurrentVolume <= 0 ||
                !Owner.InRangeUnobstructed(mopComponent.Owner))
                return false;

            // Top up mops solution given it needs it to annihilate puddles I guess

            var transferAmount = ReagentUnit.Min(mopComponent.MaxVolume - mopComponent.CurrentVolume, CurrentVolume);
            if (transferAmount == 0)
            {
                return false;
            }

            var mopContents = mopComponent.Contents;

            if (mopContents == null)
            {
                return false;
            }

            var solution = contents.SplitSolution(transferAmount);
            if (!mopContents.TryAddSolution(solution))
            {
                return false;
            }

            if (_sound != null)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _sound, Owner);
            }

            return true;
        }
    }
}
