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
bounty-console-id-label = ID#{$id}

bounty-console-flavor-left = Bounties sourced from local unscrupulous dealers.
bounty-console-flavor-right = v1.4

bounty-manifest-header = [font size=14][bold]Official cargo bounty manifest[/bold] (ID#{$id})[/font]
bounty-manifest-list-start = Item manifest:

bounty-console-tab-available-label = Available
bounty-console-tab-history-label = History
bounty-console-history-notice-completed-label = {$time} - Completed
bounty-console-history-notice-skipped-label = {$time} - Skipped by {$id}
bounty-console-history-completed-label = [color=limegreen]Completed[/color]
bounty-console-history-skipped-label = [color=red]Skipped[/color]
