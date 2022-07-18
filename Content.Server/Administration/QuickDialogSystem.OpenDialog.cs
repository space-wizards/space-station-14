using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Server.Player;

namespace Content.Server.Administration;

public sealed partial class QuickDialogSystem
{
    /// <summary>
    /// Opens a dialog for the given client, allowing them to enter in the desired data.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="title"></param>
    /// <param name="prompt"></param>
    /// <param name="okAction"></param>
    /// <param name="cancelAction"></param>
    /// <typeparam name="T1"></typeparam>
    [PublicAPI]
    public void OpenDialog<T1>(IPlayerSession session, string title, string prompt, Action<T1> okAction,
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
                try
                {
                    okAction.Invoke(ParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"]));
                }
                catch (Exception)
                {
                    session.ConnectedClient.Disconnect("Replied with invalid quick dialog data.");
                    throw;
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    [PublicAPI]
    public void OpenDialog<T1, T2>(IPlayerSession session, string title, string prompt1, string prompt2,
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
                try
                {
                    okAction.Invoke(
                        ParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"]),
                        ParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"])
                    );
                }
                catch (Exception)
                {
                    session.ConnectedClient.Disconnect("Replied with invalid quick dialog data.");
                    throw;
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    [PublicAPI]
    public void OpenDialog<T1, T2, T3>(IPlayerSession session, string title, string prompt1, string prompt2,
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
                try
                {
                    okAction.Invoke(
                        ParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"]),
                        ParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"]),
                        ParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"])
                    );
                }
                catch (Exception)
                {
                    session.ConnectedClient.Disconnect("Replied with invalid quick dialog data.");
                    throw;
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    [PublicAPI]
    public void OpenDialog<T1, T2, T3, T4>(IPlayerSession session, string title, string prompt1, string prompt2,
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
                try
                {
                    okAction.Invoke(
                        ParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"]),
                        ParseQuickDialog<T2>(TypeToEntryType(typeof(T2)), ev.Responses["2"]),
                        ParseQuickDialog<T3>(TypeToEntryType(typeof(T3)), ev.Responses["3"]),
                        ParseQuickDialog<T4>(TypeToEntryType(typeof(T4)), ev.Responses["4"])
                    );
                }
                catch (Exception)
                {
                    session.ConnectedClient.Disconnect("Replied with invalid quick dialog data.");
                    throw;
                }
            }),
            cancelAction ?? (() => { })
        );
    }
}
