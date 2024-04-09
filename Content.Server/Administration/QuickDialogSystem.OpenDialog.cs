using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Administration;

public sealed partial class QuickDialogSystem
{
    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt">The prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1>(ICommonSession session, string title, string prompt, Action<T1> okAction,
        Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1))
                    okAction.Invoke(v1);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The first prompt.</param>
    /// <param name="prompt2">The second prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the first input.</typeparam>
    /// <typeparam name="T2">Type of the second input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2>(ICommonSession session, string title, string prompt1, string prompt2,
        Action<T1, T2> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt1),
                new("2", TypeToEntryType(typeof(T2)), prompt2)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {

                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                    TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2)
                    )
                    okAction.Invoke(v1, v2);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The first prompt.</param>
    /// <param name="prompt2">The second prompt.</param>
    /// <param name="prompt3">The third prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the first input.</typeparam>
    /// <typeparam name="T2">Type of the second input.</typeparam>
    /// <typeparam name="T3">Type of the third input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3>(ICommonSession session, string title, string prompt1, string prompt2,
        string prompt3, Action<T1, T2, T3> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt1),
                new("2", TypeToEntryType(typeof(T2)), prompt2),
                new("3", TypeToEntryType(typeof(T3)), prompt3)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                    TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                    TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3)
                   )
                    okAction.Invoke(v1, v2, v3);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session">Client to show a dialog for.</param>
    /// <param name="title">Title of the dialog.</param>
    /// <param name="prompt1">The first prompt.</param>
    /// <param name="prompt2">The second prompt.</param>
    /// <param name="prompt3">The third prompt.</param>
    /// <param name="prompt4">The fourth prompt.</param>
    /// <param name="okAction">The action to execute upon Ok being pressed.</param>
    /// <param name="cancelAction">The action to execute upon the dialog being cancelled.</param>
    /// <typeparam name="T1">Type of the first input.</typeparam>
    /// <typeparam name="T2">Type of the second input.</typeparam>
    /// <typeparam name="T3">Type of the third input.</typeparam>
    /// <typeparam name="T4">Type of the fourth input.</typeparam>
    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4>(ICommonSession session, string title, string prompt1, string prompt2,
        string prompt3, string prompt4, Action<T1, T2, T3, T4> okAction, Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt1),
                new("2", TypeToEntryType(typeof(T2)), prompt2),
                new("3", TypeToEntryType(typeof(T3)), prompt3),
                new("4", TypeToEntryType(typeof(T4)), prompt4),
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1) &&
                    TryParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"], out var v2) &&
                    TryParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"], out var v3) &&
                    TryParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"], out var v4)
                   )
                    okAction.Invoke(v1, v2, v3, v4);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }
}
