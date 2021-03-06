/* The end conversation dialog thanks the user for their particpation and continues to the data profile presentation in main dialog */

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
    public class EndConversationDialog : ComponentDialog
    {
        private readonly ConversationRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        public EndConversationDialog(ConversationRecognizer luisRecognizer, ILogger<EndConversationDialog> logger)
            : base(nameof(EndConversationDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ThankTask,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        /* Thank user and return to main dialog*/
        private async Task<DialogTurnResult> ThankTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personalDetails = (PersonalDetails)stepContext.Options;
            
            await Task.Delay(1500);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($" Goodbye, have a nice day 👋🏼"), cancellationToken);
            await Task.Delay(1500);

            return await stepContext.EndDialogAsync(personalDetails, cancellationToken);

        }
    }
}
