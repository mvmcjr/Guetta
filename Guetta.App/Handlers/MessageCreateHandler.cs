using System.Threading.Tasks;
using Guetta.Abstractions;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Guetta.App.Handlers;

public class MessageCreateHandler(CommandSolverService commandSolverService, IOptions<CommandOptions> commandOptions)
    : IMessageCreateGatewayHandler
{
    public async ValueTask HandleAsync(Message message)
    {
        if (message.Content.StartsWith(commandOptions.Value.Prefix))
        {
            await commandSolverService.AddMessageToQueue(message);
        }
    }
}