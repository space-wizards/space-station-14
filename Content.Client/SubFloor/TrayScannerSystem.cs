using Content.Client.Items;
using Content.Client.Message;
using Content.Client.Power.Visualizers;
using Content.Client.Stylesheets;
using Content.Shared.Atmos.Components;
using Content.Shared.Disposal.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.SubFloor;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.SubFloor;

public sealed class TrayScannerSystem : SharedTrayScannerSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TrayScanRevealSystem _trayScanReveal = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private const string TRayAnimationKey = "trays";
    private const double AnimationLength = 0.3;

    public const LookupFlags Flags = LookupFlags.Static | LookupFlags.Sundries | LookupFlags.Approximate;

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<TrayScannerComponent>(OnCollectItemStatus);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // TODO: Multiple viewports or w/e
        var player = _player.LocalEntity;
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(player, out var playerXform))
            return;

        var playerPos = _transform.GetWorldPosition(playerXform, xformQuery);
        var playerMap = playerXform.MapID;
        var range = 0f;
        var mode = TrayScannerMode.All;
        HashSet<Entity<SubFloorHideComponent>> inRange;
        var scannerQuery = GetEntityQuery<TrayScannerComponent>();

        // TODO: Should probably sub to player attached changes / inventory changes but inventory's
        // API is extremely skrungly. If this ever shows up on dottrace ping me and laugh.
        var canSee = false;

        foreach (var item in _inventory.GetHandOrInventoryEntities(player.Value, SlotFlags.POCKET))
        {
            if (!scannerQuery.TryGetComponent(item, out var scanner) || !scanner.Enabled)
                continue;

            range = MathF.Max(scanner.Range, range);
            mode = scanner.Mode;
            canSee = true;
            break;
        }

        inRange = new HashSet<Entity<SubFloorHideComponent>>();

        if (canSee)
        {
            var entitiesInRange = new HashSet<Entity<SubFloorHideComponent>>();
            _lookup.GetEntitiesInRange(playerMap, playerPos, range, entitiesInRange, flags: Flags);

            foreach (var (uid, comp) in entitiesInRange)
            {
                if (!MatchesMode(uid, mode))
                    continue;

                inRange.Add((uid, comp));

                if (comp.IsUnderCover || _trayScanReveal.IsUnderRevealingEntity(uid))
                    EnsureComp<TrayRevealedComponent>(uid);
            }
        }

        var revealedQuery = AllEntityQuery<TrayRevealedComponent, SpriteComponent>();
        var subfloorQuery = GetEntityQuery<SubFloorHideComponent>();

        while (revealedQuery.MoveNext(out var uid, out _, out var sprite))
        {
            // Revealing
            // Add buffer range to avoid flickers.
            if (subfloorQuery.TryGetComponent(uid, out var subfloor) &&
                inRange.Contains((uid, subfloor)))
            {
                // Due to the fact client is predicting this server states will reset it constantly
                if ((!_appearance.TryGetData(uid, SubFloorVisuals.ScannerRevealed, out bool value) || !value) &&
                    sprite.Color.A > SubfloorRevealAlpha)
                {
                    _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(0f));
                }

                SetRevealed(uid, true);

                if (sprite.Color.A >= SubfloorRevealAlpha || _animation.HasRunningAnimation(uid, TRayAnimationKey))
                    continue;

                _animation.Play(uid, new Animation()
                {
                    Length = TimeSpan.FromSeconds(AnimationLength),
                    AnimationTracks =
                    {
                        new AnimationTrackComponentProperty()
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Color),
                            KeyFrames =
                            {
                                new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), 0f),
                                new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(SubfloorRevealAlpha), (float) AnimationLength)
                            }
                        }
                    }
                }, TRayAnimationKey);
            }
            // Hiding
            else
            {
                // Hidden completely so unreveal and reset the alpha.
                if (sprite.Color.A <= 0f)
                {
                    SetRevealed(uid, false);
                    RemCompDeferred<TrayRevealedComponent>(uid);
                    _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(1f));
                    continue;
                }

                SetRevealed(uid, true);

                if (_animation.HasRunningAnimation(uid, TRayAnimationKey))
                    continue;

                _animation.Play(uid, new Animation()
                {
                    Length = TimeSpan.FromSeconds(AnimationLength),
                    AnimationTracks =
                    {
                        new AnimationTrackComponentProperty()
                        {
                            ComponentType = typeof(SpriteComponent),
                            Property = nameof(SpriteComponent.Color),
                            KeyFrames =
                            {
                                new AnimationTrackProperty.KeyFrame(sprite.Color, 0f),
                                new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), (float) AnimationLength)
                            }
                        }
                    }
                }, TRayAnimationKey);
            }
        }
    }

    private void SetRevealed(EntityUid uid, bool value)
    {
        _appearance.SetData(uid, SubFloorVisuals.ScannerRevealed, value);
    }

    private bool MatchesMode(EntityUid uid, TrayScannerMode mode)
    {
        return mode switch
        {
            TrayScannerMode.All => true,
            TrayScannerMode.Wiring => HasComp<CableVisualizerComponent>(uid),
            // TODO: proper comp query after disposals refactor
            TrayScannerMode.Piping => HasComp<AtmosPipeLayersComponent>(uid) || _appearance.TryGetData(uid, DisposalTubeVisuals.VisualState, out _),
            _ => false,
        };
    }

    #region UI
    private Control OnCollectItemStatus(Entity<TrayScannerComponent> entity)
    {
        _inputManager.TryGetKeyBinding((ContentKeyFunctions.AltUseItemInHand), out var binding);
        return new StatusControl(entity, binding?.GetKeyString() ?? "");
    }

    private sealed class StatusControl : Control
    {
        private readonly RichTextLabel _label;
        private readonly TrayScannerComponent _scanner;
        private readonly string _keyBindingName;

        public StatusControl(TrayScannerComponent scanner, string keyBindingName)
        {
            _scanner = scanner;
            _keyBindingName = keyBindingName;
            _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
            AddChild(_label);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (_scanner.Enabled)
            {
                var modeLocString = _scanner.Mode switch
                {
                    TrayScannerMode.All => "tray-scanner-examine-mode-all",
                    TrayScannerMode.Wiring => "tray-scanner-examine-mode-wiring",
                    TrayScannerMode.Piping => "tray-scanner-examine-mode-piping",
                    _ => "",
                };

                _label.SetMarkup(Robust.Shared.Localization.Loc.GetString("tray-scanner-item-status-label",
                    ("mode", Robust.Shared.Localization.Loc.GetString(modeLocString)),
                    ("keybinding", _keyBindingName)));
            }
            else
            {
                _label.SetMarkup("");
            }
        }
    }
    #endregion
}
