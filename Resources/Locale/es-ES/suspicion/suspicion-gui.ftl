## SuspicionGui.xaml.cs

# Shown when clicking your Role Button in Suspicion
suspicion-ally-count-display = {$allyCount ->
    *[zero] You have no allies
    [one] Your ally is {$allyNames}
    [other] Your allies are {$allyNames}
}