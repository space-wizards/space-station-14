// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 RadsammyT <32146976+RadsammyT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._EstacaoPirata.Cards.Hand;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class CardHandComponent : Component
{
    [DataField("angle")]
    public float Angle = 120f;

    [DataField("xOffset")]
    public float XOffset = 0.5f;

    [DataField("scale")]
    public float Scale = 1;

    [DataField("limit")]
    public int CardLimit = 10;

    [DataField("flipped")]
    public bool Flipped = false;
}


[Serializable, NetSerializable]
public enum CardUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CardHandDrawMessage(NetEntity card) : BoundUserInterfaceMessage
{
    public NetEntity Card = card;
}