solution-status-transfer = Transfer: [color=white]{$volume}u[/color]

solution-status-volume = { $fillLevel ->
    [exact] Volume: [color=white]{$current}/{$max}u[/color]
   *[other] Vol: { -solution-vague-fill-level(fillLevel: $fillLevel) }
}

# For entities with a huge capacity like bluespace beakers
solution-status-volume-short = Vol: { $fillLevel ->
    [exact] [color=white]{$current}/{$max}u[/color]
   *[other] { -solution-vague-fill-level(fillLevel: $fillLevel) }
}
