using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    public class CaptureTelemetry
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly DialogContext _dialogContext;
        private readonly RecognizerResult _recognizerResult;
        private readonly IList<TelemetryDefinition> _telemetryDefinitions;
        private readonly PropertyMapper _mapper;
        private readonly ILogger _log;

        public CaptureTelemetry(
            TelemetryClient telemetryClient,
            IList<TelemetryDefinition> telemetryDefinitions,
            DialogContext dialogContext,
            RecognizerResult recognizerResult,
            ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _log = loggerFactory.CreateLogger<CaptureTelemetry>();
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _telemetryDefinitions = telemetryDefinitions ?? throw new ArgumentNullException(nameof(telemetryDefinitions));
            _dialogContext = dialogContext ?? throw new ArgumentNullException(nameof(dialogContext));
            _recognizerResult = recognizerResult;
            _mapper = new PropertyMapper(
                            _dialogContext,
                            _dialogContext.Context,
                            _recognizerResult,
                            loggerFactory
                            );
        }

        public async Task<bool> ValidateDefinitionAsync(TelemetryDefinition telemetry)
        {
            if (telemetry == null || telemetry.Fields == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(telemetry.CustomEventName))
            {
                _log.LogError("Telemetry Definition requires a custom_event_name");
                return false;
            }

            if (await _mapper.Validate(telemetry.Fields) == false)
            {
                return false;
            }

            return false;
        }

        public async Task LogEventAsync()
        {
            foreach (var def in _telemetryDefinitions)
            {
                var eventProperties = await _mapper.ResolvePropertiesAsync(def.Fields);
                _telemetryClient.TrackEvent(def.CustomEventName, eventProperties);
            }
        }

    }
}
