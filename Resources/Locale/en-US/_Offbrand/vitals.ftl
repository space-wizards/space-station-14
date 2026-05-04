offbrand-crew-monitoring-heart-rate = [color=white]{ $rate }[/color]bpm
offbrand-crew-monitoring-blood-pressure = [color=white]{ $systolic }[/color]/[color=white]{ $diastolic }[/color]
offbrand-crew-monitoring-spo2 = [color=white]{ $value }[/color]% { LOC($spo2) }

etco2-carbon-dioxide = EtCO2
etco2-ammonia = EtNH3
etco2-nitrous-oxide = EtN2O

spo2-oxygen = SpO2
spo2-nitrogen = SpN2

# vitals monitor

offbrand-vitals-spo2-value = {$value}%
offbrand-vitals-brain-activity = Brain Activity
offbrand-vitals-brain-activity-value = {$value}%

-offbrand-unit = [color=darkgray][font size=12][bold]{$unit}[/bold][/font][/color]

offbrand-vitals-air = Air
offbrand-vitals-respiratory-rate = Respiratory Rate
offbrand-vitals-respiratory-rate-value = [color=white]{$value}[/color]{-offbrand-unit(unit: " breaths/minute")}
offbrand-vitals-etco2-value = [color=white]{$value}[/color]{-offbrand-unit(unit: "mmHg")}

offbrand-vitals-blood = Blood
offbrand-vitals-blood-pressure = Blood Pressure
offbrand-vitals-blood-pressure-value = [color=white]{$systolic}[/color][color=darkgray]/[/color][color=white]{$diastolic}[/color]
offbrand-vitals-blood-volume = Blood Volume
offbrand-vitals-blood-volume-value = [color=white]{$value}[/color][color=darkgray]%[/color]

offbrand-vitals-heart = Heart
offbrand-vitals-heart-rate = Heartrate
offbrand-vitals-heart-rate-value = [color=white]{$value}[/color]{-offbrand-unit(unit: "bpm")}
