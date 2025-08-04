shared-solution-container-component-on-examine-empty-container = [color=gray]Contains no chemicals.[/color]
shared-solution-container-component-on-examine-main-text = It contains {INDEFINITE($desc)} [color={$color}]{$desc}[/color] { $chemCount ->
    [1] chemical.
   *[other] mixture of chemicals.
    }

examinable-solution-has-recognizable-chemicals = You can recognize {$recognizedString} in the solution.
examinable-solution-recognized = [color={$color}]{$chemical}[/color]

examinable-solution-on-examine-volume = The contained solution is { $fillLevel ->
    [exact] holding [color=white]{$current}/{$max}u[/color].
   *[other] { -solution-vague-fill-level(fillLevel: $fillLevel) }.
}

examinable-solution-on-examine-volume-no-max = The contained solution is { $fillLevel ->
    [exact] holding [color=white]{$current}u[/color].
   *[other] { -solution-vague-fill-level(fillLevel: $fillLevel) }.
}

-solution-vague-fill-level =
    { $fillLevel ->
        [full] [color=white]Full[/color]
        [mostlyfull] [color=white]Mostly Full[/color]
        [halffull] [color=white]Half Full[/color]
        [halfempty] [color=white]Half Empty[/color]
        [mostlyempty] [color=white]Mostly Empty[/color]
       *[empty] [color=gray]Empty[/color]
    }
