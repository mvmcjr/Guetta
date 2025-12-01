using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Microsoft.Extensions.Logging;

namespace Guetta.App
{
    public class GuildQueue : IGuildItem
    {
        public GuildQueue(ILogger<GuildQueue> logger, Voice voice, ulong guildId)
        {
            Logger = logger;
            Voice = voice;
            GuildId = guildId;
        }

        public ulong GuildId { get; }

        public int Count => Queue.Count;

        private Queue<QueueItem> Queue { get; } = new();

        private ILogger<GuildQueue> Logger { get; }

        private Task LoopQueue { get; set; }

        private Voice Voice { get; }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private CancellationTokenSource QueueCancellationTokenSource { get; set; } = new();

        private QueueItem CurrentItem { get; set; }

        private void ReOrderQueue()
        {
            var index = 0;

            foreach (var queueItem in Queue.OrderBy(i => i.CurrentQueueIndex))
            {
                queueItem.CurrentQueueIndex = index;
                index++;
            }
        }

        private void StartQueueLoop()
        {
            if (LoopQueue is { IsCompleted: true })
            {
                LoopQueue = null;
            }

            if (Queue.Count <= 0)
                return;

            if (LoopQueue != null)
            {
                if (Queue.Count > 0 && CurrentItem == null)
                {
                    QueueCancellationTokenSource.Cancel();
                    QueueCancellationTokenSource = new CancellationTokenSource();
                    LoopQueue = null;
                }
                else
                {
                    return;
                }
            }

            LoopQueue = Task.Run(async () =>
                {
                    while (Queue.TryDequeue(out var queueItem))
                    {
                        CurrentItem = queueItem;
                        queueItem.CurrentQueueIndex = 0;
                        ReOrderQueue();

                        Logger.LogInformation("Playing {@Title} requested by {@User}", queueItem.VideoInformation.Title,
                            queueItem.User.Username);
                        await Voice.Join(GuildId, queueItem.VoiceChannelId);

                        CancellationTokenSource?.Dispose();
                        CancellationTokenSource = new CancellationTokenSource();

                        queueItem.Playing = true;

                        try
                        {
                            Logger.LogInformation("Goind to play song {@Url} queued by {@Requester}",
                                queueItem.VideoInformation.Url, queueItem.User.Username);
                            await Voice.Play(queueItem, CancellationTokenSource);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Failed to play song");
                        }
                        finally
                        {
                            queueItem.Playing = false;
                            CurrentItem = null;
                        }
                    }

                    Logger.LogInformation("Queue is ending");

                    await Voice.Disconnect()
                        .ContinueWith(t =>
                        {
                            if (!t.IsCompletedSuccessfully)
                                Logger.LogError(t.Exception, "Failed to disconnect from voice");
                        });

                    Logger.LogInformation("Queue has ended");
                }, QueueCancellationTokenSource.Token)
                .ContinueWith(t =>
                {
                    LoopQueue = null;

                    if (!t.IsCompletedSuccessfully)
                    {
                        Logger.LogError(t.Exception, "Queue task has ended with an error");
                    }
                });
        }

        public IEnumerable<QueueItem> GetQueueItems()
        {
            if (CurrentItem != null) yield return CurrentItem;

            foreach (var queueItem in Queue.OrderBy(i => i.CurrentQueueIndex)) yield return queueItem;
        }

        public bool CanSkip()
        {
            return CurrentItem != null;
        }

        public void Enqueue(QueueItem item)
        {
            item.CurrentQueueIndex = Queue.Count + 1;
            Queue.Enqueue(item);
            StartQueueLoop();
        }

        public void Skip()
        {
            CancellationTokenSource?.Cancel();
        }

        public void Clear()
        {
            Queue.Clear();
            Skip();
        }
    }
}