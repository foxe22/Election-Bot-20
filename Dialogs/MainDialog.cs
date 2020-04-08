// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly ConversationRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        private readonly UserState _userState;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(UserState userState, ConversationRecognizer luisRecognizer, ElectionDialog electionDialog, UserProfileDialog userProfileDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            _userState = userState;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(userProfileDialog);
            AddDialog(electionDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await Task.Delay(3000);

            // // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "My name is BotWise, how are you today?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        public async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = await _luisRecognizer.RecognizeAsync<Luis.ElectionBot>(stepContext.Context, cancellationToken);
            var personalDetails = new PersonalDetails();

            switch (luisResult.TopIntent().intent)
            {
                case Luis.ElectionBot.Intent.discussFeeling:
                    return await stepContext.BeginDialogAsync(nameof(UserProfileDialog), personalDetails, cancellationToken);
                
                case Luis.ElectionBot.Intent.askMood:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I'm great! Thanks for asking."), cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(UserProfileDialog), personalDetails, cancellationToken);

                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string name, location, userID, voted, issues, party;

            if (stepContext.Result is PersonalDetails result)
            {
                if (result.Name == null){ 
                    name = "not disclosed";
                }
                else {
                    name = result.Name.First();
                }

                if (result.Location == null){ 
                    location = "not disclosed";
                }
                else {
                    location = result.Location.First();
                }

                if (result.UserID == null){ 
                    userID = "not disclosed";
                }
                else {
                    userID = result.UserID.First();
                }

                if (result.Voted == null){ 
                    voted = "not disclosed";
                }
                else {
                    voted = result.Voted.First();
                }

                if (result.Issues == null){ 
                    issues = "not disclosed";
                }
                else {
                    issues = result.Issues.First();
                }

                if (result.Party == null){ 
                    party = "not disclosed";
                }
                else {
                    party = result.Party.First();
                }

                var messageText = $"So, your name is {name}. You are from {location}. You {voted} in the last general election and {issues} is among the issues that you care about. You support the {party} party.";

                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            await Task.Delay(10000);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
