#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;
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
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager = default!;
#pragma warning restore 649

        public override string Name => "Bucket";

        public ReagentUnit MaxVolume
        {
            get => Contents?.MaxVolume ?? ReagentUnit.Zero;
            set
            {
                if (Contents != null)
                {
                    Contents.MaxVolume = value;
                }
            }
        }

        public ReagentUnit CurrentVolume => Contents?.CurrentVolume ?? ReagentUnit.Zero;

        private string? _sound;

        private SolutionComponent? Contents => Owner.TryGetComponent(out SolutionComponent? solution) ? solution : null;

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataFieldCached(ref _sound, "sound", "/Audio/Effects/Fluids/watersplash.ogg");
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Owner.EnsureComponent<SolutionComponent>();
        }

        private bool TryGiveToMop(MopComponent mopComponent)
        {
            if (Contents == null)
            {
                return false;
            }

            // Let's fill 'er up
            // If this is called the mop should be empty but just in case we'll do Max - Current
            var transferAmount = ReagentUnit.Min(mopComponent.MaxVolume - mopComponent.CurrentVolume, CurrentVolume);
            var solution = Contents.SplitSolution(transferAmount);
            if (!mopComponent.Contents.TryAddSolution(solution) || mopComponent.CurrentVolume == 0)
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

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (Contents == null)
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

                Owner.PopupMessage(eventArgs.User, _localizationManager.GetString("Splish"));
                return true;
            }

            var transferAmount = ReagentUnit.Min(mopComponent.CurrentVolume, MaxVolume - CurrentVolume);
            if (transferAmount == 0)
            {
                return false;
            }

            var solution = mopComponent.Contents.SplitSolution(transferAmount);
            if (!Contents.TryAddSolution(solution))
            {
                //This really shouldn't happen
                throw new InvalidOperationException();
            }

            // Give some visual feedback shit's happening (for anyone who can't hear sound)
            Owner.PopupMessage(eventArgs.User, _localizationManager.GetString("Sploosh"));

            if (_sound == null)
            {
                return true;
            }

            EntitySystem.Get<AudioSystem>().PlayFromEntity(_sound, Owner);

            return true;

        }
    }
}
