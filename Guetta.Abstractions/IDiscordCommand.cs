using System.Threading.Tasks;
using NetCord.Gateway;

namespace Guetta.Abstractions
{
    public interface IDiscordCommand
    {
        Task ExecuteAsync(Message message, string[] arguments);
    }
}