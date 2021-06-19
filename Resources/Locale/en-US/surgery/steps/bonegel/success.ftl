# Surgeon
surgery-step-bonegel-success-surgeon-popup = You successfully repair the fracture in {$target}'s {$part}
surgery-step-bonegel-success-self-surgeon-popup = You successfully repair the fracture in your {$part}
surgery-step-bonegel-success-no-zone-surgeon-popup = You successfully repair the fracture in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-bonegel-success-target-popup = {$user} successfully repairs the fracture in your {$part}

# Outsider
surgery-step-bonegel-success-outsider-popup = {$user} successfully repairs the fracture in {$target}'s {$part}
surgery-step-bonegel-success-self-outsider-popup = {$user} successfully repairs the fracture in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-bonegel-success-no-zone-outsider-popup = {$user} successfully repairs the fracture in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
