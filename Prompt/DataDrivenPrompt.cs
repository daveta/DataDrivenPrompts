// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples
{
    public class DataDrivenPrompt : TextPrompt
    {
        // Dialog State keys
        private readonly string TrainingMode;
        private readonly string PromptResult;
        private readonly BotServices _botServices;
        private readonly PromptValidator<string> _validator;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _log;

        public DataDrivenPrompt(
                    string dialogId,
                    BotServices botServices,
                    PromptConfig config,
                    ILoggerFactory loggerFactory,
                    PromptValidator<string> validator = null)
            : base(dialogId, validator)
        {
            _log = loggerFactory.CreateLogger<DataDrivenBot>();
            _botServices = botServices ?? throw new ArgumentNullException(nameof(botServices));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            // Validate all the Bot Services we need
            if (botServices.TelemetryClient == null)
            {
                throw new InvalidOperationException($"Application Insights needed to use {nameof(DataDrivenPrompt)}");
            }

            Config = config;

            if (_botServices.DispatchRecognizer == null &&
                !_botServices.LuisServices.ContainsKey(Config.Model.Name) &&
                !_botServices.QnAServices.ContainsKey(Config.Model.Name))
            {
                throw new InvalidOperationException($"A Dispatch, QnA or LUIS model must be present in your bot configuration.");
            }

            TrainingMode = $"ddTrainingMode.{config.Name}";
            PromptResult = $"ddResult.{config.Name}";
            _validator = validator;
        }

        public PromptConfig Config { get; }

        protected override async Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (Config.Type == PromptType.AdaptiveCard)
            {
                // We we need to get user's name right. Include a card.
                var activity = turnContext.Activity.CreateReply();
                activity.Attachments = new List<Attachment> { Helpers.CreateAdaptiveCardAttachment(Config.Prompt), };
                await turnContext.SendActivityAsync(activity);
            }
            else
            {
                if (isRetry && options.RetryPrompt != null)
                {
                    await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken).ConfigureAwait(false);
                }
                else if (options.Prompt != null)
                {
                    options.Prompt.Text = Config.Prompt;
                    await turnContext.SendActivityAsync(options.Prompt, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var turnContext = dc.Context;
            var activity = dc.Context.Activity;
            var state = dc.ActiveDialog.State;
            DataDrivenResult result = new DataDrivenResult();

            // Dispatch for card input
            if (string.IsNullOrWhiteSpace(activity.Text) && (activity.Value is JObject))
            {
                _log.LogInformation($"Processing card {Config.Name}..");
                var cardValue = activity.Value as JObject;
                if (Config.Type == PromptType.AdaptiveCard)
                {
                    // We retrieved card information.
                    // TODO:Match dialog and fill state slots.
                    _log.LogInformation($"Received card data. {cardValue.ToString()}");
                }
                else
                {
                    // We retrieved card information but are not in a prompt.
                    // TODO:Look for help from dialog.
                    _log.LogCritical($"Received card data outside of card prompt.  Looking for proper response in dialog.");
                }

                await dc.Context.SendActivityAsync($"Card Result: `{cardValue.ToString()}`\nTODO: Slot fill storage.\n");
                return await dc.EndDialogAsync(result);

            }

            // call LUIS and get results
            var luisResults = await _botServices.LuisServices[Config.Model.Name].RecognizeAsync(turnContext, cancellationToken);
            var topLuisIntent = luisResults.GetTopScoringIntent();
            var topIntent = topLuisIntent.intent;

            // If we dont have an intent match from LUIS, go with the intent available via
            // the on turn property (parent's LUIS model)
            if (luisResults.Intents.Count <= 0)
            {
                topIntent = "None";
            }

            result.Intent = topIntent;
            result.Updates = luisResults.Entities;

            // Determine if we matched the entity which overrides text from user.
            var text = dc.Context.Activity.Text;
            var matchedEntity = MatchEntity(luisResults.Entities);

            if (!string.IsNullOrWhiteSpace(matchedEntity))
            {
                // We've matched an entity for this prompt.
                // Override input text with the entity.  ie:
                //   Original Input: "my name is dave"
                //   Entity: "dave"
                text = matchedEntity;
            }

            // Default validation for the type given
            object promptResult = null;
            var recognizerResult = RecognizeAllTypes(matchedEntity, CultureFromActivity(dc.Context.Activity), out promptResult);
            if (recognizerResult.WasRecognized == true)
            {
                result.Type = recognizerResult.ResultType;
                result.Result = promptResult;
                _log.LogInformation($"Recognized:\nType: {result.Type}\nResult: {result.Result}\n");
            }
            else
            {
                _log.LogError($"UnRecognized:\nType: {result.Type}\nResult: {dc.Context.Activity.Text}\n");

                // Not recognized.
                result.Type = ResultType.None;
                result.Result = null;
                return new DialogTurnResult(DialogTurnStatus.Empty, result);
            }

            // Stash result in TurnContext (not currently used)
            dc.Context.TurnState["DataDrivenResult"] = result;

            // Stash result in State
            state[PromptResult] = result;

            // Perform AppInsights Storage
            await TelemetryCaptureAsync(luisResults, dc);

            if (Config.RunMode == RunModeOptions.Training)
            {
                // TODO: Save training/result data
                state[TrainingMode] = CurrentState.ConfirmingTraining;

                if (dc.Dialogs.Find("ConfirmTraining") == null)
                {
                    dc.Dialogs.Add(new ConfirmPrompt("ConfirmTraining", defaultLocale: Culture.English));
                }

                await dc.Context.SendActivityAsync($"Result Intent:\n`{result.Intent}`\nUpdates:\n`{result.Updates}`");

                await dc.PromptAsync("ConfirmTraining", new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = "Please confirm." } }, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Waiting, result);
            }

            //return new DialogTurnResult(DialogTurnStatus.Complete, result);
            return await dc.EndDialogAsync(result);
        }

        public async override Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = dc.ActiveDialog.State;

            if (result != null)
            {
                var trainingGood = (bool)result;

                // User said yes to cancel prompt.
                if (trainingGood)
                {
                    await dc.Context.SendActivityAsync("Good Results!\nTraining results logged.");
                }
                else
                {
                    await dc.Context.SendActivityAsync("Bad Results!\nTraining results logged.  TODO: In the future, explore options here to enumerate and select proper intents for training.");
                }

                return await dc.EndDialogAsync(state[PromptResult]);

                //return new DialogTurnResult(DialogTurnStatus.Complete, );
            }
            else
            {
                // User said no to cancel.
                //return await base.ResumeDialogAsync(dc, reason, result, cancellationToken);
                return await dc.EndDialogAsync(state[PromptResult]);
            }
        }

        private async Task TelemetryCaptureAsync(RecognizerResult result, DialogContext dc)
        {
            var captureTelemetry = new CaptureTelemetry(
                                            _botServices.TelemetryClient,
                                            Config.Telemetry,
                                            dc,
                                            result,
                                            _loggerFactory);
            await captureTelemetry.LogEventAsync();
        }

        private string CultureFromActivity(Activity activity)
        {
            // TODO
            return activity.Locale;
        }

        private (bool WasRecognized, ResultType ResultType) RecognizeAllTypes(string text, string culture, out object promptResult)
        {
            switch (Config.Type)
            {
                case PromptType.String:
                    promptResult = text;
                    return (true, ResultType.String);

                case PromptType.Int:
                    var recognizeResult = RecognizeInt(text, culture, out Int64 longResult);
                    if (recognizeResult)
                    {
                        promptResult = longResult;
                        return (recognizeResult, ResultType.Int);
                    }
                    else
                    {
                        promptResult = Int64.MinValue;
                        return (recognizeResult, ResultType.None);
                    }

                default:
                    throw new InvalidOperationException($"Invalid configuration : Type {Config.Type} unrecognized.");
            }
        }

        private bool RecognizeInt(string text, string culture, out Int64 longResult)
        {
            var results = NumberRecognizer.RecognizeNumber(text, culture);
            if (results.Count > 0)
            {
                if (results[0].Resolution.TryGetValue("value", out var value))
                {
                    if (value is Int32 || value is Int64)
                    {
                        longResult = (Int64)value;
                        return true;
                    }
                    else if (value is string)
                    {
                        longResult = Int64.Parse(value as string);
                        return true;
                    }
                }
            }

            longResult = -1;
            return false;
        }

        private string MatchEntity(JObject entities)
        {
            foreach (var valentity in Config.Model.MatchingEntities)
            {
                // Find the user's name from LUIS entities list.
                if (entities.TryGetValue(valentity, out var entity))
                {
                    var value = (string)entity[0];
                    return value;
                }
            }

            return null;
        }
    }
}
