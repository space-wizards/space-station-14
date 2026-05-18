bounty-console-menu-title = Cargo bounty console
bounty-console-label-button-text = Print label
bounty-console-skip-button-text = Skip
bounty-console-time-label = Time: [color=orange]{$time}[/color]
bounty-console-reward-label = Reward: [color=limegreen]${$reward}[/color]
bounty-console-manifest-label = Manifest: [color=orange]{$item}[/color]
bounty-console-manifest-entry =
    { $amount ->
        [1] {$item}
        *[other] {$item} x{$amount}
    }
bounty-console-manifest-reward = Reward: ${$reward}
bounty-console-description-label = [color=gray]{$description}[/color]
bounty-console-claim-button-text = Claim
bounty-console-claimed-by-none = None
bounty-console-claimed-by-unknown = Unknown
bounty-console-claimed-by-label = Claimed by: {$claimers}
bounty-console-status-label = Status: {$status ->
        [OnShuttle] [color=limegreen]On Shuttle[/color]
        [Waiting] Waiting
        [Undelivered] [color=orange]Undelivered[/color]
        *[other] {$status}
    }
bounty-console-status = {$status ->
        [OnShuttle] On Shuttle
        [Waiting] Waiting
        [Undelivered] Undelivered
        *[other] {$status}
    }
bounty-console-status-tooltip = {$status ->
    [OnShuttle] This bounty is on the shuttle, ready to be delivered to the trade station.
    [Waiting] This bounty is waiting to be fulfilled.
    [Undelivered] This bounty has not yet been sent out for fulfilment.
    *[other] {$status}
    }
bounty-console-id-label = ID#{$id}

bounty-console-flavor-left = Bounties sourced from local unscrupulous dealers.
bounty-console-flavor-right = v1.4

bounty-manifest-header = [font size=14][bold]Official cargo bounty manifest[/bold] (ID#{$id})[/font]
bounty-manifest-list-start = Item manifest:

bounty-console-tab-available-label = Available
bounty-console-tab-history-label = History
bounty-console-history-empty-label = No bounty history found
bounty-console-history-notice-completed-label = [color=limegreen]Completed[/color]
bounty-console-history-notice-skipped-label = [color=red]Skipped[/color] by {$id}
