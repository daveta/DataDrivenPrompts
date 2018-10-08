// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    public class DDialog : Dialog
    {
        private const string PersistedOptions = "ddOptions";
        private const string CurDialog = "ddCurDialog";
        private const string StepIndex = "ddStepIndex";
        private const string PersistedValues = "ddValues";

        private readonly DialogSet _dialogSet;
        private Dictionary<string, DataDrivenPrompt> _prompts;
        private readonly IStatePropertyAccessor<DDState> _ddStateAccessor;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _log;
        private readonly BotServices _botServices;
        private readonly string _rootDialogName;
        private readonly Dictionary<string, PromptValidator<string>> _validators;
        private readonly Dictionary<string, DialogConfig> _dialogs;

        public DDialog(
                string dialogId,
                DialogSet dialogs,
                IStatePropertyAccessor<DDState> ddStateAccessor,
                BotServices botServices,
                ILoggerFactory loggerFactory,
                string rootDialogName,
                string dialogsFileRoot,
                Dictionary<string, PromptValidator<string>> validators = null)
            : base(dialogId)
        {
            _dialogSet = dialogs;

            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _log = loggerFactory.CreateLogger<DataDrivenBot>();
            _botServices = botServices ?? throw new ArgumentNullException(nameof(botServices));
            _ddStateAccessor = ddStateAccessor ?? throw new ArgumentNullException(nameof(ddStateAccessor));
            _validators = validators ?? new Dictionary<string, PromptValidator<string>>();
            _dialogs = LoadDialogs(dialogsFileRoot);
            _rootDialogName = rootDialogName;
        }

        /// <summary>
        /// Add a new step to the waterfall.
        /// </summary>
        /// <param name="prompt">Prompt to add.</param>
        /// <returns>Waterfall dialog for fluent calls to .AddStep().</returns>
        public DDialog AddPrompt(DataDrivenPrompt prompt)
        {
            if (prompt == null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            _prompts.Add(prompt.Config.Name, prompt);
            return this;
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            // Initialize state
            var state = dc.ActiveDialog.State;
            state[PersistedOptions] = options;
            state[PersistedValues] = new Dictionary<string, object>();
            state[CurDialog] = _rootDialogName;

            // Run first step
            return await RunPromptAsync(dc, _rootDialogName, 0, DialogReason.BeginCalled, null, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            // Nothing running
            var state = dc.ActiveDialog.State;
            if (state[CurDialog] == null)
            {
                return Dialog.EndOfTurn;
            }

            // Don't do anything for non-message activities.
            if (dc.Context.Activity.Type != ActivityTypes.Message)
            {
                return Dialog.EndOfTurn;
            }

            // Run next step with the message text as the result.
            return await ResumeDialogAsync(dc, DialogReason.ContinueCalled, dc.Context.Activity.Text, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            // Increment step index and run step
            var state = dc.ActiveDialog.State;

            // For issue https://github.com/Microsoft/botbuilder-dotnet/issues/871
            // See the linked issue for details. This issue was happening when using the CosmosDB
            // data store for state. The stepIndex which was an object being cast to an Int64
            // after deserialization was throwing an exception for not being Int32 datatype.
            // This change ensures the correct datatype conversion has been done.
            var index = Convert.ToInt32(state[StepIndex]);
            var dialogName = state[CurDialog] as string;
            return await RunPromptAsync(dc, dialogName, index + 1, reason, result, cancellationToken).ConfigureAwait(false);
        }

        private async Task<DialogTurnResult> RunPromptAsync(DialogContext dc, string dialogName, int index, DialogReason reason, object result, CancellationToken cancellationToken)
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            var prompts = _dialogs[dialogName].Prompts;

            if (index < prompts.Count)
            {
                // Update persisted step index
                var state = dc.ActiveDialog.State;
                state[StepIndex] = index;

                // Create step context
                var options = state[PersistedOptions];
                var values = (IDictionary<string, object>)state[PersistedValues];
                if (index > prompts.Count)
                {
                    throw new InvalidOperationException($"Prompt index is out of range.  Index: {index}, MaxIndex: {prompts.Count}.");
                }

                if (prompts[index] == null)
                {
                    throw new InvalidOperationException($"Prompt name is null.  Index: {index}, MaxIndex: {prompts.Count}.");
                }

                if (!_prompts.ContainsKey(prompts[index]))
                {
                    throw new InvalidOperationException($"Prompt {prompts[index]} is not configured correctly.");
                }

                var config = _prompts[prompts[index]].Config;

                var act = Activity.CreateMessageActivity();
                act.Text = config.Prompt;
                var retry = Activity.CreateMessageActivity();
                retry.Text = config.RetryPrompt;
                return await dc.PromptAsync(
                    config.Name,
                    new PromptOptions
                    {
                        Prompt = (Activity)act,
                        RetryPrompt = (Activity)retry,
                    },
                    cancellationToken);

                //await _ddstate.SetAsync(turnContext, ddialogState);
                //await _conversationState.SaveChangesAsync(turnContext);
                //var stepContext = new WaterfallStepContext(this, dc, options, values, index, reason, result);
                // Execute step
                //return await OnStepAsync(stepContext, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // End of waterfall so just return any result to parent
                return await dc.EndDialogAsync(result).ConfigureAwait(false);
            }
        }

        private Dictionary<string, DialogConfig> LoadDialogs(string dialogsFileRoot)
        {
            var result = new Dictionary<string, DialogConfig>();
            var dirs = new DirectoryInfo(dialogsFileRoot);
            var alldirs = dirs.EnumerateDirectories();

            // Find and load all prompts
            var promptsConfigDir = alldirs.FirstOrDefault(x => x.Name == "Prompts");
            if (promptsConfigDir == null)
            {
                throw new InvalidOperationException($"Cannot find 'Prompts' directory in {dialogsFileRoot} folder.");
            }

            _prompts = LoadAllPrompts(promptsConfigDir);

            // Find and load the dialogs
            var dialogsConfigDirs = alldirs.Where(x => x.Name != "Prompts");
            foreach (var dialogConfigDir in dialogsConfigDirs)
            {
                var dialogConfigFiles = dialogConfigDir.EnumerateFiles();
                foreach (var dialogConfigFile in dialogConfigFiles)
                {
                    var dialogConfig = File.ReadAllText(dialogConfigFile.FullName);
                    var config = JsonConvert.DeserializeObject<DialogConfig>(dialogConfig);
                    if (config == null)
                    {
                        throw new InvalidOperationException($"Invalid dialog configuration provided in file {dialogConfigFile.FullName}.");
                    }

                    result.Add(config.Name, config);
                }
            }

            // TODO: Validate Dialogs have proper prompts

            return result;
        }

        private Dictionary<string, DataDrivenPrompt> LoadAllPrompts(DirectoryInfo promptsConfigDir)
        {
            var result = new Dictionary<string, DataDrivenPrompt>();
            foreach (var promptConfigFile in promptsConfigDir.EnumerateFiles())
            {
                var promptConfig = File.ReadAllText(promptConfigFile.FullName);
                var config = JsonConvert.DeserializeObject<PromptConfig>(promptConfig);
                if (config == null)
                {
                    throw new InvalidOperationException($"Invalid prompt configuration provided in file {promptConfigFile.FullName}.");
                }

                if (!_validators.TryGetValue(config.Name, out var validator))
                {
                    validator = null;
                }

                var newPrompt = new DataDrivenPrompt(config.Name, _botServices, config, _loggerFactory, validator);

                // Add to main dialog set
                _dialogSet.Add(newPrompt);
                result.Add(config.Name, newPrompt);
            }

            return result;
        }
    }
}