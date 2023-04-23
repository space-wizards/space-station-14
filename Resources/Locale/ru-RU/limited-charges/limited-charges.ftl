limited-charges-charges-remaining =
    { $charges ->
        [one] It has [color=fuchsia]{ $charges }[/color] charge remaining.
       *[other] It has [color=fuchsia]{ $charges }[/color] charges remaining.
    }
limited-charges-max-charges = It's at [color=green]maximum[/color] charges.
limited-charges-recharging =
    { $seconds ->
        [one] There is [color=yellow]{ $seconds }[/color] second left until the next charge.
       *[other] There are [color=yellow]{ $seconds }[/color] seconds left until the next charge.
    }
