health-analyzer-window-entity-brain-health-text = Brain Activity:
health-analyzer-window-entity-blood-pressure-text = Blood Pressure:
health-analyzer-window-entity-heart-rate-text = Heart Rate:
health-analyzer-window-entity-heart-health-text = Heart Health:
health-analyzer-window-entity-lung-health-text = Lung Health:
health-analyzer-window-entity-spo2-text = {LOC($spo2)}:
health-analyzer-window-entity-etco2-text = {LOC($etco2)}:
health-analyzer-window-entity-respiratory-rate-text = Respiratory Rate:

health-analyzer-window-entity-brain-health-value = {$value}%
health-analyzer-window-entity-heart-health-value = {$value}%
health-analyzer-window-entity-lung-health-value = {$value}%
health-analyzer-window-entity-heart-rate-value = {$value}bpm
health-analyzer-window-entity-blood-pressure-value = {$systolic}/{$diastolic}
health-analyzer-window-entity-respiratory-rate-value = {$value}breaths/minute
health-analyzer-window-entity-spo2-value = {$value}%
health-analyzer-window-entity-etco2-value = {$value}mmHg
health-analyzer-window-entity-non-medical-reagents = [color=yellow]Patient has non-medical reagents in bloodstream.[/color]

wound-bone-death = [color=red]Patient has systemic bone failure.[/color]
wound-internal-fracture = [color=red]Patient has internal fractures.[/color]
wound-incision = [color=red]Patient has open incision.[/color]
wound-clamped = [color=red]Patient has clamped arteries.[/color]
wound-retracted = [color=red]Patient has retracted skin.[/color]
wound-ribcage-open = [color=red]Patient has open ribcage.[/color]
wound-arterial-bleeding = [color=red]Patient has arterial bleeding.[/color]

etco2-carbon-dioxide = EtCO2
etco2-ammonia = EtNH3
etco2-nitrous-oxide = EtN2O

spo2-oxygen = SpO2
spo2-nitrogen = SpN2

health-analyzer-window-no-patient-damages = Patient has no injuries.

health-analyzer-status-tooltip =
    {"[bold]"}Alive[/bold]: The patient is alive and conscious.
    {"[bold]"}Critical[/bold]: The patient is unconscious and will die without intervention.
    {"[bold]"}Dead[/bold]: The patient is dead and will rot without intervention.

health-analyzer-blood-pressure-tooltip =
    A measure of how much blood is making it throughout the body.

    IV stands can be used to replenish blood volume.

    Relevant metrics:
    {"[color=#7af396]"}Blood Volume[/color]:
        Low blood volume can result in reduced blood pressure.

    {"[color=#7af396]"}Brain Activity[/color]:
        Low brain activity can result in reduced blood pressure.

    {"[color=#7af396]"}Heart Rate and Heart Health[/color]:
        Damage to the heart or a stopped heart can result in reduced blood pressure.

health-analyzer-spo2-tooltip =
    A measure of how much {LOC($gas)} is making it to the patient's body, compared to what the patient needs.

    Relevant metrics:
    {"[color=#7af396]"}Metabolic Rate[/color]:
        Physical trauma and pain can cause the body's {LOC($gas)} demand to increase.

    {"[color=#7af396]"}Blood Pressure[/color]:
        Low blood pressure can result in reduced {LOC($spo2)}.

    {"[color=#7af396]"}Lung Health[/color]:
        Low lung health can result in reduced {LOC($spo2)}.

    {"[color=#7af396]"}Asphyxiation[/color]:
        Asphyxiation can result in reduced {LOC($spo2)}.

    {"[color=#7af396]"}Respiratory Rate[/color]:
        Hyperventilation can result in the patient breathing less air per breath.

health-analyzer-heart-rate-tooltip =
    A measure of how fast the patient's heart is beating.

    The heartrate increases in response to inadequate {LOC($spo2)}.

health-analyzer-respiratory-rate-tooltip =
    A measure of how fast the patient is breathing.

    Breathing too fast can result in less air per breath, causing asphyxiation.

    Inaprovaline can encourage healthy breathing.

    Relevant metrics:
    {"[color=#7af396]"}{LOC($spo2)}[/color]:
        Inadequate access to {LOC($spo2gas)} can result in faster breathing.

    {"[color=#7af396]"}Metabolic Rate[/color]:
        Physical trauma and pain can cause the body to breathe faster.

health-analyzer-etco2-tooltip =
    A measure of how much {LOC($gas)} is being exhaled with each breath.

    Low {LOC($etco2)} can result in toxic {LOC($gas)} buildup.

    Relevant metrics:
    {"[color=#7af396]"}Respiratory Rate[/color]:
        Irregular breathing can cause the patient to not fully exhale all {LOC($gas)}.

    {"[color=#7af396]"}Blood Pressure[/color]:
        Low Blood Pressure can cause the patient to hold onto more {LOC($gas)}.

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
   *[false] {-health-analyzer-brain-health-tooltip-alive(spo2: $spo2)}
    }

-health-analyzer-brain-health-tooltip-alive =
    {"[color=#fedb79]"}Mannitol[/color] can be administered to heal brain damage if the [color=#7af396]SpO2[/color] permits.

    Relevant metrics:
    {"[color=#7af396]"}SpO2[/color]: {$spo2}%

-health-analyzer-brain-health-tooltip-dead =
    The patient has 0% brain activity and is dead.
