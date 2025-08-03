solution-status-transfer = Transfer: [color=white]{$volume}u[/color]

solution-status-volume = Vol: { $fillLevel ->
    [exact] [color=white]{$current}/{$max}u[/color]
   *[other] { -solution-vague-fill-level(fillLevel: $fillLevel) }
}
