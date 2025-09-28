-health-analyzer-rating = { $rating ->
    [good] ([color=#00D3B8]good[/color])
    [okay] ([color=#30CC19]okay[/color])
    [poor] ([color=#bdcc00]poor[/color])
    [bad] ([color=#E8CB2D]bad[/color])
    [awful] ([color=#EF973C]awful[/color])
    [dangerous] ([color=#FF6C7F]dangerous[/color])
   *[other] (unknown)
    }

health-analyzer-window-entity-brain-health-text = Brain Activity:
health-analyzer-window-entity-blood-pressure-text = Blood Pressure:
health-analyzer-window-entity-blood-oxygenation-text = Blood Saturation:
health-analyzer-window-entity-blood-flow-text = Blood Flow:
health-analyzer-window-entity-heart-rate-text = Heart Rate:
health-analyzer-window-entity-heart-health-text = Heart Health:

health-analyzer-window-entity-brain-health-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-heart-health-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-heart-rate-value = {$value}bpm { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-blood-oxygenation-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-blood-pressure-value = {$systolic}/{$diastolic} { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-blood-flow-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-non-medical-reagents = [color=yellow]Patient has non-medical reagents in bloodstream.[/color]

wound-bone-death = [color=red]Patient has systemic bone failure.[/color]
wound-internal-fracture = [color=red]Patient has internal fractures.[/color]
wound-incision = [color=red]Patient has open incision.[/color]
wound-clamped = [color=red]Patient has clamped arteries.[/color]
wound-retracted = [color=red]Patient has retracted skin.[/color]
wound-ribcage-open = [color=red]Patient has open ribcage.[/color]
wound-arterial-bleeding = [color=red]Patient has arterial bleeding.[/color]

health-analyzer-window-no-patient-damages = Patient has no injuries.
