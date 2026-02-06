shared-solution-container-component-on-examine-main-text = It contains {INDEFINITE($desc)} [color={$color}]{$desc}[/color] { $chemCount ->
    [1] chemical.
   *[other] mixture of chemicals.
    }

examinable-solution-has-recognizable-chemicals = You can recognize {$recognizedString} in the solution.
examinable-solution-recognized = [color={$color}]{$chemical}[/color]

examinable-solution-on-examine-volume = The contained solution is { $fillLevel ->
    [exact] holding [color=white]{$current}/{$max}u[/color].
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}

examinable-solution-on-examine-volume-no-max = The contained solution is { $fillLevel ->
    [exact] holding [color=white]{$current}u[/color].
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}

examinable-solution-on-examine-volume-puddle = The puddle is { $fillLevel ->
    [exact] [color=white]{$current}u[/color].
    [full] huge and overflowing!
    [mostlyfull] huge and overflowing!
    [halffull] deep and flowing.
    [halfempty] very deep.
   *[mostlyempty] pooling together.
    [empty] forming multiple small pools.
}

-solution-vague-fill-level =
    { $fillLevel ->
        [full] [color=white]Full[/color]
        [mostlyfull] [color=#DFDFDF]Mostly Full[/color]
        [halffull] [color=#C8C8C8]Half Full[/color]
        [halfempty] [color=#C8C8C8]Half Empty[/color]
        [mostlyempty] [color=#A4A4A4]Mostly Empty[/color]
       *[empty] [color=gray]Empty[/color]
    }
