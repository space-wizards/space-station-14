# Surgeon
surgery-step-vesselcompression-begin-surgeon-popup = You begin to compress vessels in {$target}'s {$part}
surgery-step-vesselcompression-begin-self-surgeon-popup = You begin to compress vessels in your {$part}
surgery-step-vesselcompression-begin-no-zone-surgeon-popup = You begin to compress vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-vesselcompression-begin-target-popup = {$user} begins to compress vessels in your {$part}

# Outsider
surgery-step-vesselcompression-begin-outsider-popup = {$user} begins to compress vessels in {$target}'s {$part}
surgery-step-vesselcompression-begin-self-outsider-popup = {$user} begins to compress vessels in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-vesselcompression-begin-no-zone-outsider-popup = {$user} begins to compress vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
