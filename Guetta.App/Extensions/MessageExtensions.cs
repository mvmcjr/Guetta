using System;
using System.Threading.Tasks;
using NetCord;
using NetCord.Rest;

namespace Guetta.App.Extensions
{
    public static class MessageExtensions
    {
        public static async Task<bool> AskReply(this RestMessage message, string content, User user, string positiveContent, Emoji positiveEmoji, string negativeContent, Emoji negativeEmoji, TimeSpan? timeoutOverride = null)
        {
            throw new NotImplementedException();
            // var positiveButton = new DiscordButtonComponent(ButtonStyle.Success, "btn_success", positiveContent, false, new DiscordComponentEmoji(positiveEmoji));
            // var negativeButton = new DiscordButtonComponent(ButtonStyle.Secondary, "btn_negative", negativeContent, false, new DiscordComponentEmoji(negativeEmoji));
            // var discordMessage = await message.RespondAsync(b => b.WithContent(content).AddComponents(negativeButton, positiveButton));
            //
            // var waitForButtonAsync = await discordMessage.WaitForButtonAsync(user, timeoutOverride);
            // await discordMessage.DeleteAsync();
            // return !waitForButtonAsync.TimedOut && waitForButtonAsync.Result.Id == "btn_success";
        }

        public static Task DeleteMessageAfter(this RestMessage message, TimeSpan timeout)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(timeout);
                await message.DeleteAsync().ContinueWith(_ => Task.CompletedTask);
            });
        }


        public static Task DeleteMessageAfter(this Task<RestMessage> message, TimeSpan timeout)
        {
            return message.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(timeout);
                        await t.Result.DeleteAsync();
                    });
                }
                else
                {
                    throw t.Exception ?? new Exception("Failed to send message");
                }
            });
        }
    }
}