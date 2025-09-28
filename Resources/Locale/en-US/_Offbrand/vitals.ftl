-crew-monitor-vitals-rating = { $rating ->
    [good] [color=#00D3B8]{$text}[/color]
    [okay] [color=#30CC19]{$text}[/color]
    [poor] [color=#bdcc00]{$text}[/color]
    [bad] [color=#E8CB2D]{$text}[/color]
    [awful] [color=#EF973C]{$text}[/color]
    [dangerous] [color=#FF6C7F]{$text}[/color]
   *[other] unknown
    }

offbrand-crew-monitoring-heart-rate = { -crew-monitor-vitals-rating(text: $rate, rating: $rating) }bpm
offbrand-crew-monitoring-blood-pressure = { -crew-monitor-vitals-rating(text: $systolic, rating: $rating) }/{ -crew-monitor-vitals-rating(text: $diastolic, rating: $rating) }
offbrand-crew-monitoring-oxygenation = { -crew-monitor-vitals-rating(text: $oxygenation, rating: $rating) }% air
