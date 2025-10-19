// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components
{
    [NetSerializable, Serializable]
    public enum RadiationCollectorVisuals
    {
        VisualState,
        TankInserted,
        PressureState,
    }

    [NetSerializable, Serializable]
    public enum RadiationCollectorVisualState
    {
        Active = (1<<0),
        Activating = (1<<1) | Active,
        Deactivating = (1<<1),
        Deactive = 0
    }
}
