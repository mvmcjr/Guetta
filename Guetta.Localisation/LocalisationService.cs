using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Guetta.Localisation.Resources;
using Microsoft.Extensions.Options;
using NetCord.Rest;

namespace Guetta.Localisation
{
    public class LocalisationService
    {
        public LocalisationService(IOptions<LocalisationOptions> options, RestClient restClient)
        {
            RestClient = restClient;
            Language.Culture = new CultureInfo(options.Value.Language);
            
            Items = typeof(Language)
                .GetProperties(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(i => i.CanRead && !i.CanWrite && i.PropertyType == typeof(string))
                .Select(i => new { i.Name, Valor = i.GetValue(null)?.ToString() })
                .Where(i => i.Valor != null)
                .ToDictionary(i => i.Name, i => i.Valor!);
        }

        private Dictionary<string, string> Items { get; }
        
        private RestClient RestClient { get; }

        public string GetMessageTemplate(string code)
        {
            var messageTemplate = Items.GetValueOrDefault(code);

            if (string.IsNullOrEmpty(messageTemplate))
            {
                throw new ArgumentOutOfRangeException(nameof(code));
            }

            return messageTemplate;
        }
        
        public Task<RestMessage> ReplyMessageAsync(RestMessage message, string code,
            params object[] parameters)
        {
            return message.ReplyAsync(string.Format(GetMessageTemplate(code), parameters));
        }
        
        public Task<RestMessage> SendMessageAsync(ulong channelId, string code,
            params object[] parameters)
        {
            return RestClient.SendMessageAsync(channelId, string.Format(GetMessageTemplate(code), parameters));
        }
        
        public Task<RestMessage> SendMessageAsync(RestMessage channel, string code,
            params object[] parameters)
        {
            return channel.SendAsync(string.Format(GetMessageTemplate(code), parameters));
        }
    }
}