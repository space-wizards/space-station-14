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
health-analyzer-window-entity-lung-health-text = Lung Health:

health-analyzer-window-entity-brain-health-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-heart-health-value = {$value}% { -health-analyzer-rating(rating: $rating) }
health-analyzer-window-entity-lung-health-value = {$value}% { -health-analyzer-rating(rating: $rating) }
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

health-analyzer-status-tooltip =
    {"[bold]"}Alive[/bold]: The patient is alive and conscious.
    {"[bold]"}Critical[/bold]: The patient is unconscious and will die without intervention.
    {"[bold]"}Dead[/bold]: The patient is dead and will rot without intervention.

health-analyzer-blood-saturation-tooltip =
    A measure of how much oxygen (or nitrogen, etc.) the patient's brain is getting.

    { $rating ->
    [good] Your patient's brain is not at risk.
    [okay] Your patient's brain may be damaged slightly.
    [poor] Your patient's brain may be damaged.
    [bad] Your patient's brain may be damaged substantially.
    [awful] Your patient's brain is at [color=red]severe risk[/color] for fatal injury.
    [dangerous] Your patient's brain is at [color=red]life-threatening risk[/color] for fatal injury.
   *[other] Your patient is an enigma. Report this to developers if you see this message.
    }

    Relevant metrics:
    {"[color=#7af396]"}Blood Pressure[/color]: {$systolic}/{$diastolic}
    {"[color=#7af396]"}Asphyxiation[/color]: {$asphyxiation}

health-analyzer-blood-pressure-tooltip =
    A measure of how much blood is in use by the body.

    If [color=#7af396]Blood Flow[/color] is high but [color=#7af396]Blood Pressure[/color] is not, ensure your patient has adequate [color=#7af396]Blood Volume[/color].

    Relevant metrics:
    {"[color=#7af396]"}Blood Flow[/color]: {$flow}%

health-analyzer-blood-flow-tooltip =
    A measure of how much the patient's body can circulate available blood.

    This primarily depends on your patient's heart having a pulse and being in good condition.
    CPR can be administered if the heart is not providing enough blood flow.

    Relevant metrics:
    {"[color=#7af396]"}Heart Rate[/color]: {$heartrate}bpm
    {"[color=#7af396]"}Heart Health[/color]: {$health}%

health-analyzer-heart-rate-tooltip =
    A measure of how fast the patient's heart is beating.

    It will raise due to pain and asphyxiation.

    It can stop due to severe pain, lack of blood, or severe brain damage.

    {"[color=#731024]"}Inaprovaline[/color] can be administered to reduce the patient's heartrate.

    Relevant metrics:
    {"[color=#7af396]"}Asphyxiation[/color]: {$asphyxiation}

health-analyzer-heart-health-tooltip =
    A measure of the heart's integrity.

    It will decrease due to excessively high heartrate.

    Relevant metrics:
    {"[color=#7af396]"}Heart Rate[/color]: {$heartrate}bpm

health-analyzer-plain-temperature-tooltip =
    The patient's body temperature.

health-analyzer-cryostasis-temperature-tooltip =
    The patient's body temperature.

    This temperature has a cryostasis factor of {$factor}%.

health-analyzer-lung-health-tooltip =
    The patient's lung health.

    The lower this number, the more difficulty they have breathing.

    If the lung health is low, consider putting the patient on higher-pressure internals.

health-analyzer-blood-tooltip =
    The patient's blood volume.

health-analyzer-damage-tooltip =
    The patient's total accumulated injuries.

health-analyzer-brain-health-tooltip = { $dead ->
    [true] {-health-analyzer-brain-health-tooltip-dead}
   *[false] {-health-analyzer-brain-health-tooltip-alive(rating: $rating, saturation: $saturation)}
    }

-health-analyzer-brain-health-tooltip-alive =
    { $rating ->
    [good] Your patient is fine, and does not need any intervention.
    [okay] Your patient has slight brain damage, but can likely heal it over time.
    [poor] Your patient has brain damage.
    [bad] Your patient has a large amount of brain damage. Administer [color=#731024]Inaprovaline[/color] to stabilize the brain before proceeding with treatment.
    [awful] Your patient has a severe amount of brain damage. [bold]Administer [color=#731024]Inaprovaline[/color] to stabilize the brain immediately.[/bold] Consider moving to a cryopod or stasis bed if you do not have a treatment plan.
    [dangerous] Your patient is at [color=red]severe risk of death[/color]. [bold]Administer [color=#731024]Inaprovaline[/color], and move the patient to a cryopod or stasis bed if you do not have a treatment plan.[/bold]
   *[other] Your patient is an enigma. Report this to developers if you see this message.
    }

    {"[color=#fedb79]"}Mannitol[/color] can be administered to heal brain damage if the [color=#7af396]Blood Saturation[/color] permits.

    Relevant metrics:
    {"[color=#7af396]"}Blood Saturation[/color]: {$saturation}%

-health-analyzer-brain-health-tooltip-dead =
    The patient has 0% brain activity and is dead.
