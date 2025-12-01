using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Guetta.Abstractions;
using Guetta.App.Exceptions;
using Guetta.App.Extensions;
using Microsoft.Extensions.Logging;

namespace Guetta.App
{
    public class YoutubeDlService
    {
        private static string[] FfmpegArguments { get; }= {
            "-hide_banner",
            "-i -",
            "-ac 2",
            "-f s16le",
            "-ar 48000",
            "-"
        };

        public YoutubeDlService(ILogger<YoutubeDlService> logger)
        {
            Logger = logger;
        }

        private ILogger<YoutubeDlService> Logger { get; }

        private static Command CreateCommand() => Cli.Wrap("yt-dlp");
        
        public async Task<bool> TryUpdate()
        {
            Logger.LogInformation("Trying to update yt-dlp");
            
            var command = CreateCommand()
                .WithValidation(CommandResultValidation.None)
                .WithArguments("-U");
            var commandExecution = await command.ExecuteBufferedAsync();

            if (commandExecution.ExitCode == 0)
            {
                Logger.LogInformation("YT-DLP is up to date {@OutputMessage}", commandExecution.StandardOutput);
                return true;
            }
            
            Logger.LogError("YT-DLP failed to update {@StdOutput} {@StdErr}", commandExecution.StandardOutput, commandExecution.StandardError);
            return false;
        }
        
        public async Task<PlaylistInformation> GetVideoInformation(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            if (!Uri.TryCreate(input, UriKind.Absolute, out _))
            {
                input = $"ytsearch:{input}";
            }
            
            var ytDlpCommand = CreateCommand()
                .WithArguments(builder => builder.Add("-J").Add("--flat-playlist").Add(input))
                .WithValidation(CommandResultValidation.None);
            var executeBufferedAsync = await ytDlpCommand.ExecuteBufferedAsync();

            if (executeBufferedAsync.ExitCode != 0)
            {
                Logger.LogError("Failed to execute YT-DLP to get video information. {@StdOut} {@StdErr}", executeBufferedAsync.StandardOutput, executeBufferedAsync.StandardError);
                return null;
            }

            var jsonDocument = JsonDocument.Parse(executeBufferedAsync.StandardOutput);
            var root = jsonDocument.RootElement;
            var returnType = root.GetProperty("_type").GetString();

            Logger.LogDebug("yt-dlp type for input {@Input} is {@Type}", input, returnType);

            switch (returnType)
            {
                case "playlist":
                {
                    var youtubeDlPlaylistInformation = JsonSerializer.Deserialize<YoutubeDlPlaylistInformation>(root.GetRawText()) ?? throw new FailedToGetVideoInformationException();

                    return new PlaylistInformation
                    {
                        Title = youtubeDlPlaylistInformation.Entries.Length < 2 ? null : youtubeDlPlaylistInformation.Title,
                        Videos = youtubeDlPlaylistInformation.Entries.Select(o => o.ToVideoInformation()).Where(i => !string.IsNullOrEmpty(i.Url)).ToArray()
                    };
                }
                case "video":
                {
                    var youtubeDlVideoInformation = JsonSerializer.Deserialize<YoutubeDlVideoInformation>(root.GetRawText()) ?? throw new FailedToGetVideoInformationException();
                    return new PlaylistInformation
                    {
                        Videos = new[] { youtubeDlVideoInformation.ToVideoInformation() }
                    };
                }
                default:
                    throw new TypeNotSupportedException(returnType);
            }
        }

        public async Task SendToAudioSink(string input, Stream currentDiscordStream,
            CancellationToken cancellationToken)
        {
            var youtubeDlArguments = new[]
            {
                "-f bestaudio*[ext=webm][acodec=opus][asr=48000]/ba*[ext=m4a]/ba*",
                "--no-continue",
                $"\"{input}\"",
                "-o -"
            };

            var youtubeDlCommand = CreateCommand()
                .WithStandardErrorPipe(PipeTarget.ToDelegate(s =>
                    Logger.LogDebug("Youtube-DL message: {@Message}", s)))
                .WithArguments(youtubeDlArguments, false);

            Logger.LogDebug("{@Program} arguments: {@Arguments}", "yt-dlp", youtubeDlArguments);
            Logger.LogDebug("{@Program} arguments: {@Arguments}", "ffmpeg", FfmpegArguments);

            var stream = new BufferedPipeStream();

            var ffmpegCommand = Cli.Wrap("ffmpeg")
                .WithStandardInputPipe(PipeSource.FromCommand(youtubeDlCommand))
                .WithStandardOutputPipe(PipeTarget.ToStream(stream))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(s => Logger.LogDebug("FFMpeg message: {@Message}", s)))
                .WithArguments(FfmpegArguments, false);

            try
            {
                var commandTask = ffmpegCommand.ExecuteAsync(cancellationToken)
                    .Task
                    .ContinueWith(_ => stream.Complete(), CancellationToken.None);

                await foreach (var bytes in stream.Reader.ReadAllAsync(cancellationToken))
                {
                    await currentDiscordStream.WriteAsync(bytes, cancellationToken);
                }

                await commandTask;
            }
            finally
            {
                await currentDiscordStream.FlushAsync(cancellationToken);
                await stream.DisposeAsync();
            }
        }
    }
}