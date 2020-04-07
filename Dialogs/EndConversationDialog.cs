// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class EndConversationDialog : ComponentDialog
    {
        public EndConversationDialog(string id)
            : base(id)
        {

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ThankTask,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ThankTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;
            
            await Task.Delay(1500);
            var messageText = $"So let's finish up before we go too far and get into an argument on political views, goodbye {personalDetails.Name}";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;
            
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here's what I gathered from our conversation: "), cancellationToken);

            return await stepContext.EndDialogAsync(personalDetails, cancellationToken);
        }
    }
}
