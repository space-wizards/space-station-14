// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Defusable;

/// <summary>
/// This handles defusable explosives, such as Syndicate Bombs.
/// </summary>
/// <remarks>
/// Most of the logic is in the server
/// </remarks>
public abstract class SharedDefusableSystem : EntitySystem
{

}

[NetSerializable, Serializable]
public enum DefusableVisuals
{
    Active
}

[NetSerializable, Serializable]
public enum DefusableWireStatus
{
    LiveIndicator,
    BoltIndicator,
    BoomIndicator,
    DelayIndicator,
    ProceedIndicator,
}
