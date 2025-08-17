// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._vg.TileMovement;

/// <summary>
/// When attached to an entity with an InputMoverComponent, all mob movement on that entity will
/// be tile-based. Contains info used to facilitate that movement.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TileMovementComponent : Component
{
    /// <summary>
    /// Whether a tile movement slide is currently in progress.
    /// </summary>
    [AutoNetworkedField]
    public bool SlideActive;

    /// <summary>
    /// Local coordinates from which the current slide first began.
    /// </summary>
    [AutoNetworkedField]
    public EntityCoordinates Origin;

    /// <summary>
    /// Local coordinates of the target of the current slide.
    /// </summary>
    [AutoNetworkedField]
    public Vector2 Destination;

    /// <summary>
    /// This helps determine how long a slide should last. A slide will continue so long
    /// as a movement key (WASD) is being held down, but if it was held down for less than
    /// a certain time period then it will continue for a minimum period.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan? MovementKeyInitialDownTime;

    /// <summary>
    /// Move buttons used to initiate the current slide.
    /// </summary>
    [AutoNetworkedField]
    public MoveButtons CurrentSlideMoveButtons;

    /// <summary>
    /// Whether this entity was weightless last physics tick.
    /// </summary>
    [AutoNetworkedField]
    public bool WasWeightlessLastTick;

    /// <summary>
    /// Whether the current ongoing slide was initiated due to a failed slide.
    /// </summary>
    [AutoNetworkedField]
    public bool FailureSlideActive;

    /// <summary>
    /// Coordinates of the moving entity on the last physics tick. Null if the entity was not
    /// parented to the same entity last tick.
    /// </summary>
    [AutoNetworkedField]
    public Vector2? LastTickLocalCoordinates;
}