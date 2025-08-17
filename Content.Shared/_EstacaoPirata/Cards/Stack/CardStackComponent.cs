// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2024 RadsammyT <32146976+RadsammyT@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._EstacaoPirata.Cards.Stack;

/// <summary>
/// This is used for holding the prototype ids of the cards in the stack or hand.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]

public sealed partial class CardStackComponent : Component
{
    [DataField("content")]
    public List<EntProtoId> InitialContent = [];

    [DataField("shuffleSound")]
    public SoundSpecifier ShuffleSound = new SoundCollectionSpecifier("cardFan");

    [DataField("pickUpSound")]
    public SoundSpecifier PickUpSound = new SoundCollectionSpecifier("cardSlide");

    [DataField("placeDownSound")]
    public SoundSpecifier PlaceDownSound = new SoundCollectionSpecifier("cardShove");


    /// <summary>
    /// The containers that contain the items held in the stack
    /// </summary>
    [ViewVariables]
    public Container ItemContainer = default!;

    /// <summary>
    /// The list EntityUIds of Cards
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntityUid> Cards = [];
}

[Serializable, NetSerializable]
public sealed class CardStackInitiatedEvent(NetEntity cardStack) : EntityEventArgs
{
    public NetEntity CardStack = cardStack;
}

/// <summary>
/// This gets Updated when new cards are added or removed from the stack
/// </summary>
[Serializable, NetSerializable]
public sealed class CardStackQuantityChangeEvent(NetEntity stack, NetEntity? card, StackQuantityChangeType type) : EntityEventArgs
{
    public NetEntity Stack = stack;
    public NetEntity? Card = card;
    public StackQuantityChangeType Type = type;
}

[Serializable, NetSerializable]
public enum StackQuantityChangeType : sbyte
{
    Added,
    Removed,
    Joined,
    Split
}



[Serializable, NetSerializable]
public sealed class CardStackReorderedEvent(NetEntity stack) : EntityEventArgs
{
    public NetEntity Stack = stack;
}

[Serializable, NetSerializable]
public sealed class CardStackFlippedEvent(NetEntity cardStack) : EntityEventArgs
{
    public NetEntity CardStack = cardStack;
}


