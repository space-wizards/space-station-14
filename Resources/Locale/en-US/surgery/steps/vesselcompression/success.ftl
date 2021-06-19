# Surgeon
surgery-step-vesselcompression-success-surgeon-popup = You compress vessels in {$target}'s {$part}
surgery-step-vesselcompression-success-self-surgeon-popup = You compress vessels in your {$part}
surgery-step-vesselcompression-success-no-zone-surgeon-popup = You compress vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-vesselcompression-success-target-popup = {$user} compresses vessels in your {$part}

# Outsider
surgery-step-vesselcompression-success-outsider-popup = {$user} compresses vessels in {$target}'s {$part}
surgery-step-vesselcompression-success-self-outsider-popup = {$user} compresses vessels in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-vesselcompression-success-no-zone-outsider-popup = {$user} compresses vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
