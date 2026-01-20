### UI

# Displayed in the Character prefs window
humanoid-character-profile-summary =
    This is {$name}. {$subject ->
    [male] He
    [female] She
    [epicene] They
    [neuter] It
    *[other] {CAPITALIZE($subject)}
} {$conjugate-be ->
    [true] are
    *[other] {CONJUGATE-BE($conjugate-be)}
} {$age} years old.
