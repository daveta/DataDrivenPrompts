using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// PropertyManager resolves properties passed in a configuration definition
    /// to values.
    /// </summary>
    public class PropertyMapper
    {
        private readonly DialogContext _dialogContext;
        private readonly ITurnContext _context;
        private readonly RecognizerResult _recognizerResult;
        private readonly ILogger _log;

        private Dictionary<string, HashSet<string>> _propertyLookup = new Dictionary<string, HashSet<string>>
        {
            { "Activity", new HashSet<string> { "LocalTimestamp", "Timestamp", "ServiceUrl",
                "Id", "Type", "ChannelId", "ReplyToId", "Text", "Locale", "Speak", "HistoryDisclosed",
                "TopicName", "AttachmentLayout", "TextFormat", "InputHint", "Summary",
                "DeliveryMode", "Importance", "Expiration", "Code", } },
            { "Activity.Recipient",  new HashSet<string> { "Id", "Name", "Role", "Properties" } },
            { "Activity.From",  new HashSet<string> { "Id", "Name", "Role", "Properties" } },
            { "RecognizerResult", new HashSet<string> { "Entities", "Intents" } }, 
            { "RecognizerResult.Intents", null }, // Disable explicity property name checks
        };

        public PropertyMapper(
            DialogContext dialogContext,
            ITurnContext context,
            RecognizerResult recognizerResult,
            ILoggerFactory loggerFactory)
        {

            _log = loggerFactory?.CreateLogger<PropertyMapper>() ?? throw new ArgumentNullException(nameof(ILoggerFactory));
            _dialogContext = dialogContext;
            if (_dialogContext != null)
            {
                _context = _dialogContext.Context;
            }
            else
            {
                _context = context;
            }

            _recognizerResult = recognizerResult;
        }

        /// <summary>
        /// Retrieve all the requested data and place into a dictionary.
        /// </summary>
        /// <remarks>Assumed validation has already been performed.</remarks>
        /// <param name="properties"></param>
        /// <returns></returns>
        public Task<Dictionary<string, string>> ResolvePropertiesAsync(List<string> properties)
        {
            var result = new Dictionary<string, string>();

            foreach (var property in properties)
            {
                var tokens = property.Split(" ");
                if (tokens.Count() == 1)
                {
                    var value = RetrievePropertyValue(tokens[0], out var propertyName);
                    result.Add(propertyName, value);
                }

                if (tokens.Count() == 3)
                {
                    var value = RetrievePropertyValue(tokens[0], out var propertyName);
                    result.Add(tokens[2], value);
                }
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Retrieve the requested property as a string.
        /// </summary>
        /// <param name="fullPropertyName">The full property in the form ClassName[.NestedClass].PropertyName</param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private string RetrievePropertyValue(string fullPropertyName, out string propertyName)
        {
            var lastDotIndex = fullPropertyName.LastIndexOf(".");
            propertyName = fullPropertyName.Substring(lastDotIndex + 1);

            switch (fullPropertyName.Substring(0, lastDotIndex).ToLowerInvariant())
            {
                case "activity":
                    var interimResult = typeof(Activity).GetProperty(propertyName).GetValue(_context.Activity);
                    return interimResult.ToString();
                case "recognizerresult":
                    var recognizerIntentOrEntity = typeof(RecognizerResult).GetProperty(propertyName).GetValue(_recognizerResult);
                    return JsonConvert.SerializeObject(recognizerIntentOrEntity);
                case "recognizerresult.intents.score":
                    return _recognizerResult.Intents[propertyName].Score.ToString();
                case "recognizerresult.intents":
                    return JsonConvert.SerializeObject(_recognizerResult.Intents);

                default:
                    return null;
            }
        }

        public async Task<Boolean> Validate(List<string> properties)
        {
            foreach (var property in properties)
            {
                if (await ValidateProperty(property) == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validate a single property.
        /// </summary>
        /// <remarks>
        /// Accepted syntax:
        ///    Class.Property[ as newName].
        /// For example, `Activity.Text as text` is a valid property.
        /// </remarks>
        /// <param name="property">A string of the form ClassName.PropertyName[ as NewName].</param>
        /// <returns>True if the class and property can be retrieved; false otherwise.</returns>
        public async Task<Boolean> ValidateProperty(string property)
        {
            var tokens = property.Split(" ");
            if (tokens.Count() == 1)
            {
                return await ValidateClassProperty(tokens[0]);
            }

            if (tokens.Count() == 3)
            {
                if (string.Compare(tokens[1], "as", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // Validate the new name
                    return true;
                }
            }

            _log.LogInformation($"Property {property} does not conform to `ClassName.PropertyName[ as NewName]`.");

            return false;
        }

        /// <summary>
        /// Validates syntax for Class.Property.
        /// </summary>
        /// <param name="fullName">A string of the form ClassName.PropertyName</param>
        /// <returns>True if the class and property can be retrieved; false otherwise.</returns>
        private async Task<Boolean> ValidateClassProperty(string fullName)
        {
            var parts = fullName.Split(".");
            if (parts.Count() != 2)
            {
                _log.LogError($"Property {fullName} is not of the form <class>.<property>.");
                return false;
            }

            if (!_propertyLookup.ContainsKey(parts[0]))
            {
                _log.LogError($"Property {fullName} references class {parts[0]} which isn't known.");
                return false;
            }

            if (!_propertyLookup[parts[0]].Contains(parts[1]))
            {
                _log.LogError($"Property {fullName} references property {parts[0]} which isn't known (Case-sensitive).");
                return false;
            }

            return true;
        }
    }
}
