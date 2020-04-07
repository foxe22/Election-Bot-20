using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ConstituencyDialog : ComponentDialog
    {
        private readonly ConversationRecognizer _luisRecognizer;
        protected readonly ILogger Logger;

        public ConstituencyDialog(ConversationRecognizer luisRecognizer, ILogger<ConstituencyDialog> logger)
            : base(nameof(ConstituencyDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskConstituency,
                RemarkOnLocationAsync,
                AgreeStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskConstituency(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;
            
            if (personalDetails.Location == null)
            {
                var messageText = "My local voting constituency is in Wicklow. Where's your's then?";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
            
            var luisResult = await _luisRecognizer.RecognizeAsync<Luis.ElectionBot>(stepContext.Context, cancellationToken);
            return await stepContext.NextAsync(personalDetails.Location, cancellationToken);
        }

        private async Task<DialogTurnResult> RemarkOnLocationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;
            var luisResult = await _luisRecognizer.RecognizeAsync<Luis.ElectionBot>(stepContext.Context, cancellationToken);
            personalDetails.Location = luisResult.Entities.location;

            if(luisResult.Entities.location == null) {
                var messageText = $"I see, I see. Suprising result in general, wasn't it?";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(ConstituencyDialog), personalDetails, cancellationToken);
            }
            else {
                if(personalDetails.Location.First() == "wexford") {
                    var messageText = $"The Sunny South East! A big win for Johnny Mythen down there, a suprising result don't you think?";
                    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
                else if (personalDetails.Location.First() == "dublin" || personalDetails.Location.First() == "Dun Laoighre") {
                    var messageText = $"Interesting. Dublin's poll was dominated by Sinn Féin with 24% of the preference. Suprising result don't you think?";
                    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
                else if (personalDetails.Location.First() == "carlow" || personalDetails.Location.First() == "kilkenny") {
                    var messageText = $"Interesting. A big win for Kathleen Funchion in the Carlow-Kilkenny constituency.  An unsuprrising result don't you think?";
                    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
                else if (personalDetails.Location.First() == "donegal") {
                    var messageText = $"Very good. A big win for Sinn Féin's Pearse Doherty in the Donegal area. An unsuprrising result don't you think?";
                    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
                else if (personalDetails.Location.First() == "galway") {
                    var messageText = $"Very good. A big result for the independent Seán Canney in Galway. A suprrising result don't you think?";
                    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
                else {
                    var messageText = $"Very good. A big win for Pearse Doherty in the Donegal area. A suprrising result don't you think?";
                    var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
            }

            return await stepContext.NextAsync(personalDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> AgreeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;
            
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Yeah I thought so too."), cancellationToken);

            return await stepContext.EndDialogAsync(personalDetails, cancellationToken);
        }
    }
}
