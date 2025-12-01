using System;
using System.Linq;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.App;
using Guetta.App.Extensions;
using Guetta.Localisation;
using NetCord.Gateway;
using NetCord.JsonModels;
using NetCord.Rest;

namespace Guetta.Commands
{
    internal class PlayChannelCommand(
        LocalisationService localisationService,
        GuildContextManager guildContextManager,
        VideoInformationService videoInformationService,
        RestClient restClient)
        : IDiscordCommand
    {
        public async Task ExecuteAsync(Message message, string[] arguments)
        {
            if (message.Channel == null)
                return;

            if (arguments.Length < 1)
            {
                await localisationService
                    .ReplyMessageAsync(message, "InvalidArgument", message.Author.Username)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
                return;
            }

            if (!message.GuildId.HasValue)
            {
                await message.Channel.SendMessageAsync("Invalid guild ID in channel");
                return;
            }

            var guildContext = guildContextManager.GetOrCreate(message.GuildId.Value);
            var queue = guildContext.GuildQueue;

            var voiceState = await restClient.GetGuildUserVoiceStateAsync(message.GuildId.Value, message.Author.Id);

            if (!voiceState.ChannelId.HasValue)
            {
                await localisationService
                    .ReplyMessageAsync(message, "NotInChannel", message.Author.Username)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
                return;
            }

            await restClient.TriggerTypingStateAsync(message.Channel.Id);
            var url = arguments.Aggregate((x, y) => $"{x} {y}");

            var playlistInformation = await videoInformationService.GetVideoInformation(url);
            var queueItems = playlistInformation?.Videos
                .Select(i => new QueueItem
                {
                    User = new QueueUser(message.Author.Id, message.Author.Username),
                    TextChannelId = message.ChannelId,
                    VoiceChannelId = voiceState.ChannelId.Value,
                    VideoInformation = i
                }).ToArray();

            if (queueItems is { Length: > 1 })
            {
                // var positiveEmoji = new JsonEmoji();
                // var negativeEmoji = DiscordEmoji.FromName(DiscordClient, ":x:");
                // var content = string.Format(localisationService.GetMessageTemplate("MultipleSongsConfirmation"),
                //     queueItems.Length, positiveEmoji, negativeEmoji);
                // var confirmPlaylistQueue = await message.AskReply(content, message.Author,
                //     localisationService.GetMessageTemplate("MultipleSongsConfirmationPositiveButton"), positiveEmoji,
                //     localisationService.GetMessageTemplate("MultipleSongsConfirmationNegativeButton"), negativeEmoji,
                //     TimeSpan.FromSeconds(10));


                await localisationService.ReplyMessageAsync(message, "MultipleSongsConfirmationCanceled")
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
                return;
            }

            if (queueItems != null)
                foreach (var queueItem in queueItems)
                    queue.Enqueue(queueItem);

            if (queueItems is { Length: > 0 } && string.IsNullOrEmpty(playlistInformation.Title))
            {
                await localisationService
                    .ReplyMessageAsync(message, "SongQueued", message.Author.Username, string.Empty)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
            }
            else if (queueItems is { Length: > 0 })
            {
                await localisationService
                    .ReplyMessageAsync(message, "PlaylistQueued", message.Author.Username, playlistInformation.Title)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
            }
            else
            {
                await localisationService
                    .ReplyMessageAsync(message, "SongNotFound", message.Author.Username, string.Empty)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(5));
            }
        }
    }
}