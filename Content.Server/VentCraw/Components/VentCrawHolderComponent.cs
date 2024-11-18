using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.VentCraw.Components
{
    [RegisterComponent]
    public sealed class VentCrawHolderComponent : Component
    {
        public Container Container = null!;

        [ViewVariables]
        public float StartingTime { get; set; }

        [ViewVariables]
        public float TimeLeft { get; set; }

        public bool IsMoving = false;

        [ViewVariables]
        public EntityUid? PreviousTube { get; set; }
        [ViewVariables]
        public EntityUid? NextTube { get; set; }

        [ViewVariables]
        public Direction PreviousDirection { get; set; } = Direction.Invalid;

        [ViewVariables]
        public EntityUid? CurrentTube { get; set; }

        [ViewVariables]
        public bool FirstEntry { get; set; }

        [ViewVariables]
        public Direction CurrentDirection { get; set; } = Direction.Invalid;

        [ViewVariables]
        public bool IsExitingVentCraws { get; set; }

        public static readonly TimeSpan CrawlDelay = TimeSpan.FromSeconds(0.5);

        public TimeSpan LastCrawl;

        [DataField("crawlSound")]
        public SoundCollectionSpecifier CrawlSound { get; } = new ("VentClaw", AudioParams.Default.WithVolume(5f));

        [DataField("speed")]
        public float Speed = 0.15f;
    }
}
