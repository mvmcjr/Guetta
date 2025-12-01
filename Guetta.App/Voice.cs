using System;
using System.Threading;
using System.Threading.Tasks;
using Guetta.Abstractions;
using Guetta.App.Extensions;
using Guetta.Localisation;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Rest;
using LogLevel = NetCord.Logging.LogLevel;

namespace Guetta.App
{
    internal record PlayRequest(QueueItem QueueItem, CancellationTokenSource CancellationToken);

    public class Voice(
        YoutubeDlService youtubeDlService,
        LocalisationService localisationService,
        ulong guildId,
        ILogger<Voice> logger,
        GatewayClient gatewayClient,
        RestClient restClient)
        : IGuildItem
    {
        public ulong GuildId { get; } = guildId;


        private YoutubeDlService YoutubeDlService { get; } = youtubeDlService;

        public VoiceClient? AudioClient { get; set; }
        
        public ulong? AudioChannelId { get; set; }

        private LocalisationService LocalisationService { get; } = localisationService;

        private ILogger<Voice> Logger { get; } = logger;

        public Task ChangeVolume(double newVolume)
        {
            // if(AudioClient?.GetTransmitSink() is { } transmitSink)
            //     transmitSink.VolumeModifier = newVolume;

            return Task.CompletedTask;
        }

        public async Task Join(ulong guildId, ulong voiceChannel)
        {
            AudioClient = await gatewayClient.JoinVoiceChannelAsync(
                guildId,
                voiceChannel,
                new VoiceClientConfiguration
                {
                    Logger = new ConsoleLogger(LogLevel.Debug)
                });

            await AudioClient.StartAsync();
            await AudioClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone));
            
            AudioChannelId = voiceChannel;
        }

        public async Task Disconnect()
        {
            if (AudioClient != null)
            {
                await gatewayClient.UpdateVoiceStateAsync(new VoiceStateProperties(GuildId, null));
                
                await AudioClient.CloseAsync();
                AudioClient.Dispose();
                AudioClient = null;
                
                AudioChannelId = null;
            }
        }

        public Task Play(QueueItem queueItem, CancellationTokenSource cancellationToken) =>
            Play(new PlayRequest(queueItem, cancellationToken));

        private async Task Play(PlayRequest playRequest)
        {
            if(AudioClient == null)
                throw new InvalidOperationException("Audio client is null");
            
            try
            {
                await restClient.TriggerTypingStateAsync(playRequest.QueueItem.TextChannelId);
                
                await LocalisationService.SendMessageAsync(playRequest.QueueItem.TextChannelId, "SongPlaying",
                        playRequest.QueueItem.VideoInformation.Title, playRequest.QueueItem.User.Username)
                    .DeleteMessageAfter(TimeSpan.FromSeconds(15));
                
                await using var outStream = AudioClient.CreateOutputStream();
                await using OpusEncodeStream opusStream = new(outStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);

                await YoutubeDlService.SendToAudioSink(playRequest.QueueItem.VideoInformation.Url, opusStream,
                    playRequest.CancellationToken.Token);
            }
            catch (Exception ex)
            {
                if (!playRequest.CancellationToken.IsCancellationRequested)
                    Logger.LogError(ex, "Failed to play something");
            }
        }
    }
}