// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class DataDrivenBot : IBot
    {
        private const string WelcomeText = "This bot will introduce you to data driven prompts. Try typing `hello` to get started.";

        private readonly BotServices _services;
        private readonly ConversationState _conversationState;
        private readonly IStatePropertyAccessor<DialogState> _dialogState;
        private readonly IStatePropertyAccessor<PromptStateDD> _promptstate;
        private readonly IStatePropertyAccessor<DDState> _ddstate;
        private readonly ILogger _log;
        private readonly IStatePropertyAccessor<DDState> _ddStateAccessor;
        /// <summary>
        /// The <see cref="DialogSet"/> that contains all the Dialogs that can be used at runtime.
        /// </summary>
        private readonly DialogSet _dialogs;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDrivenBot"/> class.
        /// </summary>
        /// <param name="accessors">The state accessors this instance will be needing at runtime.</param>
        public DataDrivenBot(BotServices services, ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _log = loggerFactory.CreateLogger<DataDrivenBot>();
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _dialogState = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _promptstate = _conversationState.CreateProperty<PromptStateDD>(nameof(PromptStateDD));
            _ddstate = _conversationState.CreateProperty<DDState>(nameof(DDState));
            _dialogs = new DialogSet(_dialogState);
            _dialogs.Add(new TextPrompt("name", CustomPromptValidatorAsync));
            _dialogs.Add(new ConfirmPrompt("ConfirmTraining", defaultLocale: Culture.English));

            _dialogs.Add(new DDialog(nameof(DDialog), _dialogs, _ddstate, _services, loggerFactory, "greeting", @".\Dialogs"));
            // Old Stuff
            //var namePrompt = File.ReadAllText(@".\Prompt\PromptName.json");
            //_dialogs.Add(new DataDrivenPrompt("namePrompt", services, namePrompt, _promptstate, loggerFactory));
            //var agePrompt = File.ReadAllText(@".\Prompt\PromptAge.json");
            //_dialogs.Add(new DataDrivenPrompt("agePrompt", services, agePrompt, _promptstate, loggerFactory));
            //var cardPrompt = File.ReadAllText(@".\Prompt\PromptCard.json");
            //_dialogs.Add(new DataDrivenPrompt("cardPrompt", services, cardPrompt, _promptstate, loggerFactory));
        }

        /// <summary>
        /// This controls what happens when an <see cref="Activity"/> gets sent to the bot.
        /// </summary>
        /// <param name="turnContext">Provides the <see cref="ITurnContext"/> for the turn of the bot.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            // We are only interested in Message Activities.
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Run the DialogSet - let the framework identify the current state of the dialog from
                // the dialog stack and figure out what (if any) is the active dialog.
                var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                var ddialogState = await _ddstate.GetAsync(turnContext, () => new DDState());

                // If the DialogTurnStatus is Empty we should start a new dialog.
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var result = results.Result as DataDrivenResult;

                    // Handle interruptions
                    if (result != null && result.Intent == "Cancel")
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text($"Canceling get user name activity!"), cancellationToken);
                    }

                    await dialogContext.PromptAsync(
                        nameof(DDialog),
                        new PromptOptions
                                    {
                                        Prompt = null,
                                        RetryPrompt = null,
                                    },
                        cancellationToken);

                //    switch (ddialogState.PromptIndex)
                //    {
                //        case 0:
                //            {
                //                var act = Activity.CreateMessageActivity();
                //                act.Text = "What is your name?";
                //                var retry = Activity.CreateMessageActivity();
                //                retry.Text = "A name must be more than three characters in length. Please try again.";
                //                await dialogContext.PromptAsync(
                //                    nameof(DDialog),
                //                    null,
                //                    cancellationToken);
                //            }

                //            break;

                //        case 1:
                //            {
                //                var act = Activity.CreateMessageActivity();
                //                act.Text = "What is your age?";
                //                var retry = Activity.CreateMessageActivity();
                //                retry.Text = "A name must be more than three characters in length. Please try again.";

                //                await dialogContext.PromptAsync(
                //                    "agePrompt",
                //                    new PromptOptions
                //                    {
                //                        Prompt = (Activity)act,
                //                        RetryPrompt = (Activity)retry,
                //                    },
                //                    cancellationToken);
                //            }

                //            break;

                //        case 2:
                //            {
                //                await dialogContext.PromptAsync(
                //                    "cardPrompt",
                //                    null,
                //                    cancellationToken);
                //            }

                //            break;
                //    }

                //    await _ddstate.SetAsync(turnContext, ddialogState);
                //    await _conversationState.SaveChangesAsync(turnContext);

                }

                // We had a dialog run (it was the prompt) now it is Complete.
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    // Check for a result.
                    if (results.Result != null)
                    {
                        var result = results.Result as DataDrivenResult;

                        // Handle interruptions
                        if (result.Intent == "Cancel")
                        {
                            await turnContext.SendActivityAsync(MessageFactory.Text($"Canceling get user name activity!"), cancellationToken);
                        }

                        // Handle prompt-specific stuff
                        switch (ddialogState.PromptIndex)
                        {
                            case 0:
                                // And finish by sending a message to the user. Next time ContinueAsync is called it will return DialogTurnStatus.Empty.
                                await turnContext.SendActivityAsync(MessageFactory.Text($"Thank you, I have your name as '{result.Result as string}'."), cancellationToken);
                                await dialogContext.PromptAsync(
                                    "agePrompt",
                                    new PromptOptions
                                    {
                                        Prompt = MessageFactory.Text("What is your Age?"),
                                        RetryPrompt = MessageFactory.Text("A name must be more than three characters in length. Please try again."),
                                    },
                                    cancellationToken);

                                break;

                            case 1:
                                var age = (Int64)result.Result;

                                // And finish by sending a message to the user. Next time ContinueAsync is called it will return DialogTurnStatus.Empty.
                                await turnContext.SendActivityAsync(MessageFactory.Text($"Thank you, I have your age as '{age}'."), cancellationToken);
                                await dialogContext.PromptAsync(
                                    "cardPrompt",
                                    new PromptOptions
                                    {
                                        Prompt = MessageFactory.Text("What is your Age?"),
                                        RetryPrompt = MessageFactory.Text("A name must be more than three characters in length. Please try again."),
                                    },
                                    cancellationToken);

                                break;

                            case 2:
                                // And finish by sending a message to the user. 
                                await turnContext.SendActivityAsync(MessageFactory.Text($"Thank you, this is where we'd book your table where certain slots are filled.  Clearing state."), cancellationToken);
                                await turnContext.SendActivityAsync(MessageFactory.Text($"Try saying `hello`."), cancellationToken);
                                // Reset dialog state
                                ddialogState = new DDState();
                                ddialogState.PromptIndex = 2;
                                break;

                        }

                        // Bump to next prompt.
                        ddialogState.PromptIndex = (ddialogState.PromptIndex + 1) % 3;
                        await _ddstate.SetAsync(turnContext, ddialogState);
                        await _conversationState.SaveChangesAsync(turnContext);
                    }
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Send a welcome message to the user and tell them what actions they may perform to use this bot
                await SendWelcomeMessageAsync(turnContext, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }

            // Save the new turn count into the conversation state.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        /// <summary>
        /// This is an example of a custom validator. This example can be directly used on a float NumberPrompt.
        /// Returning true indicates the recognized value is acceptable. Returning false will trigger re-prompt behavior.
        /// </summary>
        /// <param name="promptContext">The <see cref="PromptValidatorContext"/> gives the validator code access to the runtime, including the recognized value and the turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        public Task<bool> CustomPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var result = promptContext.Recognized.Value;

            // This condition is our validation rule.
            if (result != null && result.Length > 3)
            {
                // You are free to change the value you have collected. By way of illustration we are simply uppercasing.
                var newValue = result.ToUpperInvariant();

                promptContext.Recognized.Value = newValue;

                // Success is indicated by passing back the value the Prompt has collected. You must pass back a value even if you haven't changed it.
                return Task.FromResult(true);
            }

            // Not calling End indicates validation failure. This will trigger a RetryPrompt if one has been defined.

            // Note you are free to do async IO from within a validator. Here we had no need so just complete.
            return Task.FromResult(false);
        }

        /// <summary>
        /// On a conversation update activity sent to the bot, the bot will
        /// send a message to the any new user(s) that were added.
        /// </summary>
        /// <param name="turnContext">Provides the <see cref="ITurnContext"/> for the turn of the bot.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        $"Welcome to PromptValidationBot {member.Name}. {WelcomeText}",
                        cancellationToken: cancellationToken);
                }
            }
        }
    }
}
