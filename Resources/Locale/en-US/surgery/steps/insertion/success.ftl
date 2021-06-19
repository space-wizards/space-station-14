# Surgeon
surgery-step-insertion-success-surgeon-popup = You insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {$target}'s {$part}
surgery-step-insertion-success-self-surgeon-popup = You insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into your {$part}
surgery-step-insertion-success-no-zone-surgeon-popup = You insert {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-insertion-success-target-popup = {$user} inserts {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into your {$part}

# Outsider
surgery-step-insertion-success-outsider-popup = {$user} inserts {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {$target}'s {$part}
surgery-step-insertion-success-self-outsider-popup = {$user} inserts {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-insertion-success-no-zone-outsider-popup = {$user} inserts {PROPER($item) ->
  [false] the
  *[bucket] {""}
} {$item} into {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
