// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared.Fax;

[Serializable, NetSerializable]
public enum FaxMachineVisuals : byte
{
    VisualState,
}

[Serializable, NetSerializable]
public enum FaxMachineVisualState : byte
{
    Normal,
    Inserting,
    Printing
}
