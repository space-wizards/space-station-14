# interaction
comp-crew-medal-inspection-text = Awarded to {$recipient} for {$reason}.
comp-crew-medal-award-text = {$recipient} has been awarded the {$medal}.

# round end screen

comp-crew-medal-round-end-result = {$count ->
    [one] There was one medal awarded:
    *[other] There were {$count} medals awarded:
}

comp-crew-medal-round-end-list =
    - [color=white]{$recipient}[/color] earned the [color=white]{$medal}[/color] for
    {"  "}{$reason}

# UI

crew-medal-ui-header = Medal Settings
crew-medal-ui-reason = Reason for award:
crew-medal-ui-character-limit = {$number}/{$max}
crew-medal-ui-info = This can no longer be changed once you award this medal to someone.
crew-medal-ui-save = Save
