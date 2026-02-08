using System.Numerics;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.GPS.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Shared.GPS.Systems;

public sealed class HandheldGpsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldGPSComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HandheldGPSComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HandheldGPSComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    /// <summary>
    /// Handles showing the coordinates when a GPS is examined.
    /// </summary>
    private void OnExamine(Entity<HandheldGPSComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(GetGpsDisplayMarkup(ent, abbreviated: false));
    }

    private void OnUseInHand(Entity<HandheldGPSComponent> entity, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ToggleMode(entity, args.User);
        args.Handled = true;
    }

    private void OnGetVerbs(Entity<HandheldGPSComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanComplexInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("handheld-gps-verb-change-mode"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => ToggleMode(entity, user),
            Impact = LogImpact.Low,
        });
    }

    /// <summary>
    /// Toggles <see cref="HandheldGPSComponent.Mode"/> and plays a boop sound for feedback to the user.
    /// </summary>
    private void ToggleMode(Entity<HandheldGPSComponent> entity, EntityUid? user)
    {
        _audio.PlayLocal(
            entity.Comp.ModeChangeSound,
            entity,
            user,
            AudioParams.Default
                .WithVolume(1.5f)
                .WithPitchScale(entity.Comp.Mode == HandheldGpsMode.GridRelativeEntityCoordinates ? 0.8f : 1.0f)
        );

        entity.Comp.Mode = entity.Comp.Mode == HandheldGpsMode.GridRelativeEntityCoordinates
            ? HandheldGpsMode.MapCoordinates
            : HandheldGpsMode.GridRelativeEntityCoordinates;
        Dirty(entity);
    }

    /// <summary>
    /// Returns a a markup string containing something like "Coords:\n($x, $y)" depending on localization. This should
    /// be used anywhere the "readout" of the GPS is needed.
    /// </summary>
    public string GetGpsDisplayMarkup(Entity<HandheldGPSComponent> ent, bool abbreviated)
    {
        var mode = ent.Comp.Mode == HandheldGpsMode.GridRelativeEntityCoordinates
            ? "handheld-gps-examine-mode-grid-relative-entity-coords"
            : "handheld-gps-examine-mode-map-coords";
        if (abbreviated)
            mode += "-abbreviated";

        var coordinates = ent.Comp.Mode switch
        {
            HandheldGpsMode.MapCoordinates => GetMapCoordsOrNull(ent)?.Position,
            HandheldGpsMode.GridRelativeEntityCoordinates => GetEntityCoordsOrNull(ent)?.Position,
            _ => null,
        };
        var coordinatesText = coordinates switch
        {
            var (x, y) => Loc.GetString("handheld-gps-coordinates-pair", ("x", (int)x), ("y", (int)y)),
            null => Loc.GetString("handheld-gps-coordinates-error"),
        };

        return Loc.GetString("handheld-gps-coordinates-title",
            ("mode", Loc.GetString(mode)),
            ("coordinates", coordinatesText));
    }

    private MapCoordinates? GetMapCoordsOrNull(Entity<HandheldGPSComponent> ent)
    {
        var pos = _transform.GetMapCoordinates(ent);
        return pos.MapId == MapId.Nullspace ? null : pos;
    }

    private EntityCoordinates? GetEntityCoordsOrNull(Entity<HandheldGPSComponent> ent)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryComp(ent, out var transform) ||
            transform.GridUid is not { } gridUid)
            return null;

        return new EntityCoordinates(
            gridUid,
            Vector2.Transform(
                _transform.GetWorldPosition(transform),
                _transform.GetInvWorldMatrix(
                    xformQuery.GetComponent(transform.GridUid.Value),
                    xformQuery
                )
            )
        );
    }
}
