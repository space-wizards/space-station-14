#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Fluids
{
    /// <summary>
    /// Can a mop click on this entity and dump its fluids
    /// </summary>
    [RegisterComponent]
    public class BucketComponent : Component, IInteractUsing
    {
        public override string Name => "Bucket";

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

        private string? _sound;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _sound, "sound", "/Audio/Effects/Fluids/watersplash.ogg");
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponentWarn<SolutionContainerComponent>();
        }

        private bool TryGiveToMop(MopComponent mopComponent)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? contents))
            {
                return false;
            }

            var mopContents = mopComponent.Contents;

            if (mopContents == null)
            {
                return false;
            }

            // Let's fill 'er up
            // If this is called the mop should be empty but just in case we'll do Max - Current
            var transferAmount = ReagentUnit.Min(mopComponent.MaxVolume - mopComponent.CurrentVolume, CurrentVolume);
            var solution = contents.SplitSolution(transferAmount);
            if (!mopContents.TryAddSolution(solution) || mopComponent.CurrentVolume == 0)
            {
                return false;
            }

            if (_sound == null)
            {
                return true;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity(_sound, Owner);

            return true;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent? contents))
            {
                return false;
            }

            if (!eventArgs.Using.TryGetComponent(out MopComponent? mopComponent))
            {
                return false;
            }

            // Give to the mop if it's empty
            if (mopComponent.CurrentVolume == 0)
            {
                if (!TryGiveToMop(mopComponent))
                {
                    return false;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("Splish"));
                return true;
            }

            var transferAmount = ReagentUnit.Min(mopComponent.CurrentVolume, MaxVolume - CurrentVolume);
            if (transferAmount == 0)
            {
                return false;
            }

            var mopContents = mopComponent.Contents;

            if (mopContents == null)
            {
                return false;
            }

            var solution = mopContents.SplitSolution(transferAmount);
            if (!contents.TryAddSolution(solution))
            {
                //This really shouldn't happen
                throw new InvalidOperationException();
            }

            // Give some visual feedback shit's happening (for anyone who can't hear sound)
            Owner.PopupMessage(eventArgs.User, Loc.GetString("Sploosh"));

            if (_sound == null)
            {
                return true;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity(_sound, Owner);

            return true;
        }
    }
}
