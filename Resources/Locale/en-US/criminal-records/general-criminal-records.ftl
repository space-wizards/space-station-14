general-criminal-record-console-window-title = Criminal Records Computer
general-criminal-record-console-select-record-info = Select a record on the left.
general-criminal-record-console-empty-state = No records found!
general-criminal-record-console-no-record-found = No record was found for the selected person.

general-criminal-record-arrest-button = Arrest
general-criminal-record-release-button = Release

general-criminal-record-reason-placeholder = Reason

general-criminal-record-permission-denied = Permission denied

general-criminal-record-console-record-age = Age: {$age}
general-criminal-record-console-record-title = Job: {$job}
general-criminal-record-console-record-species = Species: {$species}
general-criminal-record-console-record-gender = Gender: {$gender}
general-criminal-record-console-record-fingerprint = Fingerprint: {$fingerprint}
general-criminal-record-console-record-dna = DNA: {$dna}
general-criminal-record-console-record-status = Status: {$status ->
    *[none]    None
    [wanted]   [color=red]Wanted[/color]
    [detained] [color=dodgerblue]Detained[/color]
}

## Security channel notifications
### On Arrest/Release button pressed

general-criminal-record-console-detained = {$name} has been detained {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

general-criminal-record-console-released = {$name} has been released from the detention {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

### On status changed

general-criminal-record-console-wanted = {$name} is wanted {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

general-criminal-record-console-not-wanted = {$name} is not wanted anymore {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

## Filters

general-criminal-record-for-filter-line-placeholder = Input text and press "Enter"
general-criminal-record-name-filter = Name of person
general-criminal-record-prints-filter = Fingerprints
general-criminal-record-dna-filter = DNA
general-criminal-record-console-search-records = Search
general-criminal-record-console-reset-filters = Reset
