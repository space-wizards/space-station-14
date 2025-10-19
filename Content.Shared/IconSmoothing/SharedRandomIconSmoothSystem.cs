// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.IconSmoothing;

public abstract class SharedRandomIconSmoothSystem : EntitySystem
{
}
[Serializable, NetSerializable]
public enum RandomIconSmoothState : byte
{
    State
}
