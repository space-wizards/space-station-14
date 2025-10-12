health-change-display =
    { $deltasign ->
        [-1] [color=green]{ NATURALFIXED($amount, 2) }[/color] ед. { $kind }
       *[1] [color=red]{ NATURALFIXED($amount, 2) }[/color] ед. { $kind }
    }
