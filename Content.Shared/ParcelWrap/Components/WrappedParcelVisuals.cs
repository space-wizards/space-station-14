// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.ParcelWrap.Components;

/// <summary>
/// This enum is used to change the sprite used by WrappedParcels based on the parcel's size.
/// </summary>
[Serializable, NetSerializable]
public enum WrappedParcelVisuals : byte
{
    Size,
    Layer,
}
