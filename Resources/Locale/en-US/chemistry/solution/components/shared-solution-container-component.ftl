shared-solution-container-component-on-examine-empty-container = [color=gray]Contains no chemicals.[/color]
shared-solution-container-component-on-examine-main-text = It contains {INDEFINITE($desc)} [color={$color}]{$desc}[/color] { $chemCount ->
    [1] chemical.
    *[other] mixture of chemicals.
    }

drink-examine-sicko-hours = Vol: { $fillLevel ->
    [full] [color=white]Full[/color]
    [mostlyFull] [color=white]Mostly Full[/color]
    [halfFull] [color=white]Mostly Full[/color]
    [halfEmpty] [color=white]Half Empty[/color]
    [mostlyEmpty] [color=white]Mostly Empty[/color]
    [exact] [color=white]{$current}/{$max}u[/color]
    *[empty] [color=gray]Empty[/color]
    }

# Legacy names
drink-component-on-examine-is-full = Vol: [color=white]Full[/color]
drink-component-on-examine-is-mostly-full = Vol: [color=white]Mostly Full[/color]
drink-component-on-examine-is-half-full = Vol: [color=white]Half Full[/color]
drink-component-on-examine-is-half-empty = Vol: [color=white]Half Empty[/color]
drink-component-on-examine-is-mostly-empty = Vol: [color=white]Mostly Empty[/color]
drink-component-on-examine-is-empty = Vol: [color=gray]Empty[/color]
drink-component-on-examine-exact-volume = Volume: [color=white]{$current}/{$max}u[/color]

examinable-solution-has-recognizable-chemicals = You can recognize {$recognizedString} in the solution.
examinable-solution-recognized-first = [color={$color}]{$chemical}[/color]
examinable-solution-recognized-next = , [color={$color}]{$chemical}[/color]
# Special handling to include a space at the start of the line
examinable-solution-recognized-last = {" "}and [color={$color}]{$chemical}[/color]
