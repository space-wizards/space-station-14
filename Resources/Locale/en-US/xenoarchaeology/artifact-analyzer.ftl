analysis-console-menu-title = Broad-Spectrum Mark 3 Analysis Console
analysis-console-server-list-button = Server
analysis-console-extract-button = Extract points

analysis-console-info-no-scanner = No analyzer connected! Please connect one using a multitool.
analysis-console-info-no-artifact = No artifact present! Place one on the pad to view node information.
analysis-console-info-ready = Systems operational. Ready to scan.

analysis-console-no-node = Select node to view
analysis-console-info-id = [font="Monospace" size=11]ID:[/font]
analysis-console-info-id-value = [font="Monospace" size=11][color=yellow]{$id}[/color][/font]
analysis-console-info-class = [font="Monospace" size=11]Class:[/font]
analysis-console-info-class-value = [font="Monospace" size=11]{$class}[/font]
analysis-console-info-locked = [font="Monospace" size=11]Status:[/font]
analysis-console-info-locked-value = [font="Monospace" size=11][color={ $state ->
    [0] red]Locked
    [1] lime]Unlocked
    *[2] plum]Active
}[/color][/font]
analysis-console-info-durability = [font="Monospace" size=11]Durability:[/font]
analysis-console-info-durability-value = [font="Monospace" size=11][color={$color}]{$current}/{$max}[/color][/font]
analysis-console-info-effect = [font="Monospace" size=11]Effect:[/font]
analysis-console-info-effect-value = [font="Monospace" size=11][color=gray]{ $state ->
    [true] {$info}
    *[false] Unlock nodes to gain info
}[/color][/font]
analysis-console-info-trigger = [font="Monospace" size=11]Triggers:[/font]
analysis-console-info-triggered-value = [font="Monospace" size=11][color=gray]{$triggers}[/color][/font]
analysis-console-info-scanner = Scanning...
analysis-console-info-scanner-paused = Paused.
analysis-console-progress-text = {$seconds ->
    [one] T-{$seconds} second
    *[other] T-{$seconds} seconds
}

analysis-console-extract-value = [font="Monospace" size=11][color=orange]Node {$id} (+{$value})[/color][/font]
analysis-console-extract-none = [font="Monospace" size=11][color=orange] No unlocked nodes have any points left to extract [/color][/font]
analysis-console-extract-sum = [font="Monospace" size=11][color=orange]Total Research: {$value}[/color][/font]

analyzer-artifact-extract-popup = Energy shimmers on the artifact's surface!
