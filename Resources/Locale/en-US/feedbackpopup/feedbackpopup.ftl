feedbackpopup-window-name = Request for feedback

feedbackpopup-control-button-text = Open Link
feedbackpopup-control-ui-type = Survey

feedbackpopup-control-total-surveys = {$num ->
    [one] {$num} survey
   *[other] {$num} surveys
}
feedbackpopup-control-ui-footer = Let us what you think!

# Command strings
feedbackpopup-command-name = givefeedbackpopup
cmd-givefeedbackpopup-desc = Gives the targeted player a feedback popup.
cmd-givefeedbackpopup-help = Usage: givefeedbackpopup <playerUid> <prototypeId>
feedbackpopup-command-error-invalid-proto = Invalid feedback popup prototype.
feedbackpopup-command-error-popup-send-fail = Couldn't send popup. There probably isn't a mind attached to the given entity.
feedbackpopup-command-success = Sent popup!
feedbackpopup-command-hint-playerUid = <playerUid>
feedbackpopup-command-hint-protoId = <prototypeId>
