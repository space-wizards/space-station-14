cmd-jobs-slot =
    { $station } job {$job} : { $mode ->
        [slots] { $slots }
        [infinite] infinite
        *[other] (not a slot)
    }
