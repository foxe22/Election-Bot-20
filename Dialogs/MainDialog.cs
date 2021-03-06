
/* The main dialog runs as the first component in the conversation dialog. It contains 4 StepAsyncs:
        1. Intro step - which uses the phrase 'wake bot' as input
        2. Act step - finds the LUIS intent for the first user input
        3. Final Step - when all dialogs have been looped through, this runs at the end which prints out the users data profile
        4. End Step - Asks the user to continue via the 'next step' button on the user website 
*/

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
                EndStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        /*Runs the first async which ensures that the user uses the 'wake bot' phrase*/
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await Task.Delay(1000);

            // // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "Hi there, thanks for waking me up! 😴 My name is BotWise, hope you are well! So first of all, to begin then...";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        /* Returns an error if the user doesn't use the 'wake bot' phrase, continues otherwise */
        public async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var luisResult = await _luisRecognizer.RecognizeAsync<Luis.ElectionBot>(stepContext.Context, cancellationToken);
            var personalDetails = new PersonalDetails();    // creates a new instance of the personal details class which will be used to save data varaibles disclosed by the user. See PersonalDetails.cs

            // If the user uses the 'wake bot' command, continue to the 'UserProfileDialog', otherwise, ask users to restart the process.
            switch (luisResult.TopIntent().intent)
            {
                case Luis.ElectionBot.Intent.wakeBot:
                    return await stepContext.BeginDialogAsync(nameof(UserProfileDialog), personalDetails, cancellationToken); //Pass the cancellation token and personalDetails object
                
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please use 'wake bot' to wake the bot. Refresh the page and restart";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        /* Prints out user inferences based on entities contained in PersonalDetails object*/
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

                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here's what I could gather from our conversation: "), cancellationToken);

                /** -------------------- NAME --------------------- **/

                await Task.Delay(1000);

                if(name == "not disclosed"){
                    await Task.Delay(1);
                }
                else{
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"So first of all, your name is {name}"), cancellationToken);
                }

                /** -------------------- VOTING --------------------- **/

                await Task.Delay(1000);

                if(voted == "did vote"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You voted in the last general election, which probably means that you have an interest in politics and care about your right to vote."), cancellationToken);
                }
                else if(voted == "did not vote"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You did not vote in the last general election, which means you probably feel indifferent about politics, or don't have the right to vote in Ireland."), cancellationToken);
                }

                /** -------------------- LOCATION --------------------- **/

                await Task.Delay(2000);

                if(location == "not disclosed"){
                    await Task.Delay(1);
                }
                else {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I found out that you're from {location}"), cancellationToken);
                }

                /** -------------------- ISSUES --------------------- **/
                
                await Task.Delay(1000);

                if(issues == "education"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can infer that you are either a teacher or a student, who cares about improving education in ireland."), cancellationToken);
                }
                else if(issues == "housing"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Improving housing is important to you, I can infer that you are paying expensive rent in dublin as a student or finding it difficult to find affordable housing."), cancellationToken);
                }
                else if(issues == "teacher's pay"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you're probably a teacher because you care about getting equal pay"), cancellationToken);
                }
                else if(issues == "health service"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Improving health infrastructure is important to you so or someone you know have probably experienced long waiting times in hospitals recently."), cancellationToken);

                    await Task.Delay(1000);
                    
                    if(location == "wexford"){
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Plus the fact that your from Wexford may mean that you feel that a 24/7 cardiac care unit is needed in the county."), cancellationToken);
                    }
                }
                else if(issues == "coronavirus"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're worried about the coronavirus and the implications it may cause for society. There's a chance you could be part of an 'at risk' health group."), cancellationToken);
                }
                else if(issues == "mortgage"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're worried about the mortgage situation, I can assume that you might be building a house in the future."), cancellationToken);
                }
                else if(issues == "climate change"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're worried about the environment and think it's important that we act together to stop climate change."), cancellationToken);
                }
                else if(issues == "public transport"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Improving Ireland's public transport indrastructure is important to you... You probably don't drive a car and you're a student."), cancellationToken);
                }
                else if(issues == "unemployment"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Improving Ireland's employment rates are important to you which may mean that you might be unemployed at the minute."), cancellationToken);
                }
                else if(issues == "mental health"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Improving mental health infrastructure in Ireland is important to you"), cancellationToken);
                    
                    await Task.Delay(1000);

                    if(location == "wexford"){
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Plus the fact that your from Wexford may mean that you are in favour of St. Sennan's reopening as a psychiatric unit."), cancellationToken);
                    }
                }

                /** -------------------- PARTY --------------------- **/

                await Task.Delay(2000);

                if(party == "not disclosed"){
                    await Task.Delay(1);
                }
                else if(party == "green party" || party == "greens"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You support the Green Party so I can tell that you care deeply about stopping climate change is an number one priority for you."), cancellationToken);

                    await Task.Delay(2000);

                    if(voted == "did vote"){
                        if(location == "wexford"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Paula Roseingrave or Verona Murphy your top votes."), cancellationToken);
                        }
                        else if(location == "dun - laoghaire" || location == "dun laoighre" || location == "dun - laoighre" || location == "dún - laoghaire" || location == "dún laoighre"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Ossian Smyth your top vote."), cancellationToken);
                        }
                        else if(location == "dublin central" || location == "dublin - central"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Nesa Hourigan your top vote."), cancellationToken);
                        }
                        else if(location == "galway"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Seán Canney your top vote."), cancellationToken);
                        }
                        else if(location == "kildare"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Ronan Maher your top vote."), cancellationToken);
                        }
                        else if(location == "cork"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Liam Quade your top vote."), cancellationToken);
                        }
                        else if(location == "leitrim"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Martin Kenny your top vote."), cancellationToken);
                        }
                        else if(location == "carlow" || location == "carlow-kilkenny" || location == "kilkenny"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Malcom Noonan your top vote."), cancellationToken);
                        }
                        else if(location == "cavan"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Rosín Garvey your top vote."), cancellationToken);
                        }
                        else if(location == "mayo"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Saoirse McHugh your top vote."), cancellationToken);
                        }
                        else if(location == "louth"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Mark Deary your top vote."), cancellationToken);
                        }
                        else if(location == "dublin south west" || location == "dublin south-west" || location == "dublin south - west"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Nesa Hourigan your top vote."), cancellationToken);
                        }
                    }
                }
                /** -------------------- SINN FEIN --------------------- **/

                else if(party == "sinn fein" || party == "sinn féin" || party == "SF"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You support Sinn Féin, so I can tell that you're interested in a fairer, equal society for everyone. Establishing a united Ireland may also be important to you."), cancellationToken);
                
                    await Task.Delay(2000);

                    if(voted == "did vote"){
                        if(location == "wexford"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Johnny Myhten your number one vote."), cancellationToken);
                        }
                        else if(location == "dun - laoghaire" || location == "dun laoighre" || location == "dun - laoighre" || location == "dún - laoghaire" || location == "dún laoighre"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Shane O'Brien your number one vote."), cancellationToken);
                        }
                        else if(location == "galway"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Louis O'Hara or Seán Canney your number one vote."), cancellationToken);
                        }
                        else if(location == "kildare"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Patricia Ryan your number one vote."), cancellationToken);
                        }
                        else if(location == "donegal"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Pearse Doherty your number one vote."), cancellationToken);
                        }
                        else if(location == "leitrim"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Martin Kenny your number one vote."), cancellationToken);
                        }
                        else if(location == "cork"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Pat Buckley your number one vote."), cancellationToken);
                        }
                        else if(location == "cavan"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Violet-Anne Wynne your number one vote."), cancellationToken);
                        }
                        else if(location == "mayo"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Rose Conway-Walsh your number one vote."), cancellationToken);
                        }
                        else if(location == "carlow" || location == "carlow-kilkenny" || location == "kilkenny"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Kathleen Funchion your number one vote."), cancellationToken);    
                        }
                        else if(location == "louth"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Imelda Munster your number one vote."), cancellationToken);
                        }
                        else if(location == "dublin central" || location == "dublin - central"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Mary Lou McDonald your number one vote."), cancellationToken);
                        }
                    }
                }

                 /** -------------------- FIANNA FAIL --------------------- **/
                else if(party == "fianna fail" || party == "fianna fáil" || party == "FF"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You support Fianna Fáil. You are in favour of realistic government policies that are more in touch with the people. You'd never vote for a Fine Gael candidate."), cancellationToken);
                
                    await Task.Delay(2000);

                    if(voted == "did vote"){
                        if(location == "wexford"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given James Browne your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "dun - laoghaire" || location == "dun laoighre" || location == "dun - laoighre" || location == "dún - laoghaire" || location == "dún laoighre"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Cormac Devlin your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "galway"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Anne Rabbitte your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "kildare"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Fiona O'Loughlin your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "donegal"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Charlie McConalouge your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "leitrim"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Marc MacSharry your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "cork"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given James O'Connor your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "cavan"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Cathal Crowe your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "mayo"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Dara Calleary your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "louth"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Declan Breathnach your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "carlow" || location == "carlow-kilkenny" || location == "kilkenny"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given John McGuinness your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "dublin central" || location == "dublin - central"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Mary Fitzpatrick your number one vote in the last election."), cancellationToken);
                        }
                    }
                }
                 /** -------------------- FINE GAEL --------------------- **/
                else if(party == "fine gael"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You support Fine Gael. You think an 'econonmy driven' Ireland is the best approach for government. You'd rather Ireland remains part of Europe in the future."), cancellationToken);

                    await Task.Delay(2000);

                    if(voted == "did vote"){
                        if(location == "wexford"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Paul Kehoe or Micahel D'Arcy your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "dun - laoghaire" || location == "dun laoighre" || location == "dun - laoighre" || location == "dún - laoghaire" || location == "dún laoighre"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Jennifer Carroll MacNeill or Mary Mitchell O'Connor your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "galway"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Ciaran Cannon your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "kildare"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Martin Héydon your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "donegal"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Joe McHugh your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "leitrim"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Frankie Feighan your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "cork"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given David Stanton your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "cavan"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Joe Carey your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "mayo"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Michael Ring your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "carlow" || location == "carlow-kilkenny" || location == "kilkenny"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given John Paul Phelan your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "louth"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Fergus O'Dowd your number one vote in the last election."), cancellationToken);
                        }
                    }
                }

                 /** -------------------- LABOUR --------------------- **/
                
                else if(party == "labour"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You support Labour party. You believe in an equal society for everyone in society."), cancellationToken);

                    await Task.Delay(2000);

                    if(voted == "did vote"){
                        if(location == "cavan"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Brendan Howlin your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "cork"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Séan Sherlock your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "carlow" || location == "carlow - kilkenny" || location == "kilkenny"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Denis Hynes your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "louth"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you may have given Ged Nash your number one vote in the last election."), cancellationToken);
                        }
                    }
                }

                /** -------------------- INDEPENDENTS --------------------- **/

                else if(party == "independent" || party == "independents"){
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You see yourself as an independent which possibly means you have no interest in mainstream politics and don't neccesarily align to a political party."), cancellationToken);
                
                    await Task.Delay(2000);

                    if(voted == "did vote"){
                        if(location == "wexford"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Verona Murphy your number one vote in the last election."), cancellationToken);
                        }
                        else if(location == "dun - laoghaire" || location == "dun laoighre" || location == "dun - laoighre" || location == "dún - laoghaire" || location == "dún laoighre"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Richard Boyd Barrett and Ossian Smyth a number 1 or 2 vote in the last election."), cancellationToken);
                        }
                        else if(location == "galway"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Seán Canney a number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "kildare"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Cathal Berry a number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "donegal"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave John O'Donnell a number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "leitrim"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Marian Harkin a number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "cork"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Mary Linehan-Foley your number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "mayo"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Gráinne DeBara your number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "cavan"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Michael MacNamara your number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "carlow" || location == "carlow-kilkenny" || location == "kilkenny"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Alan Hynes your number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "louth"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Peter Fitzpatrick your number 1 vote in the last election."), cancellationToken);
                        }
                        else if(location == "dublin central" || location == "dublin - central"){
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I can guess that you voted left in the last election and probably gave Mary Lou or Gary Gannon your number 1 vote in the last election."), cancellationToken);
                        }
                    }
                }
            }

            await Task.Delay(2000);    

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Anyway that's everything from me!"), cancellationToken);
            
            var messageText = stepContext.Options?.ToString() ?? "Is it ok if I save this information? Yes or No?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> EndStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //await stepContext.Context.SendActivityAsync(MessageFactory.Text($"That's cool with me"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Please use the 'Next Step' button to continue..."), cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
