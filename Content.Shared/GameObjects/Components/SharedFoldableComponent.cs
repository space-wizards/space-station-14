using System;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    /// <summary>
    /// Makes an object foldable. That means in its normal state, the object can't be picked.
    /// When clicked, it is folded, and it can now be picked and placed back. This is useful for
    /// wheelchairs, foldable chairs, rollerbeds etc.
    /// </summary>
    public abstract class SharedFoldableComponent : Component
    {
        public override string Name => "Foldable";

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsFolded
        {
            get => _isFolded;
            set
            {
                if (_isFolded == value) return;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                    appearance.SetData(FoldableVisuals.FoldedState, value);
                _isFolded = value;
            }
        }

        public bool CanBeFolded => !Owner.IsInContainer();

        private bool _isFolded = false;


        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(FoldableVisuals.FoldedState, _isFolded);
        }
    }

    [Serializable, NetSerializable]
    public enum FoldableVisuals : byte
    {
        FoldedState
    }
}
