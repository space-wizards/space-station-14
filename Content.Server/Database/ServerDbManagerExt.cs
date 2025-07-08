using System.Text.Json;
using Robust.Shared.Asynchronous;

namespace Content.Server.Database;

public static class ServerDbManagerExt
{
    /// <summary>
    /// Subscribe to a database notification on a specific channel, formatted as JSON.
    /// </summary>
    /// <param name="dbManager">The database manager to subscribe on.</param>
    /// <param name="taskManager">The task manager used to run the main callback on the main thread.</param>
    /// <param name="sawmill">Sawmill to log any errors to.</param>
    /// <param name="channel">
    /// The notification channel to listen on. Only notifications on this channel will be handled.
    /// </param>
    /// <param name="action">
    /// The action to run on the notification data.
    /// This runs on the main thread.
    /// </param>
    /// <param name="earlyFilter">
    /// An early filter callback that runs before the JSON message is deserialized.
    /// Return false to not handle the notification.
    /// This does not run on the main thread.
    /// </param>
    /// <param name="filter">
    /// A filter callback that runs after the JSON message is deserialized.
    /// Return false to not handle the notification.
    /// This does not run on the main thread.
    /// </param>
    /// <typeparam name="TData">The type of JSON data to deserialize.</typeparam>
    public static void SubscribeToJsonNotification<TData>(
        this IServerDbManager dbManager,
        ITaskManager taskManager,
        ISawmill sawmill,
        string channel,
        Action<TData> action,
        Func<bool>? earlyFilter = null,
        Func<TData, bool>? filter = null)
    {
        dbManager.SubscribeToNotifications(notification =>
        {
            if (notification.Channel != channel)
                return;

            if (notification.Payload == null)
            {
                sawmill.Error($"Got {channel} notification with null payload!");
                return;
            }

            if (earlyFilter != null && !earlyFilter())
                return;

            TData data;
            try
            {
                data = JsonSerializer.Deserialize<TData>(notification.Payload)
                       ?? throw new JsonException("Content is null");
            }
            catch (JsonException e)
            {
                sawmill.Error($"Got invalid JSON in {channel} notification: {e}");
                return;
            }

            if (filter != null && !filter(data))
                return;

            taskManager.RunOnMainThread(() =>
            {
                action(data);
            });
        });
    }
}
