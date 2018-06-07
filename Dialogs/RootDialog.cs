using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SupportBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        // Initial dialog begins - first waits for a question from the user
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        // Message is received from the user
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;

            // Forward the message to QnA to get the appropriate response to the user's question and send it back to them.
            await context.Forward(child: new BasicQnAMakerDialog(), resume: AfterAnswerAsync, item: message, token: CancellationToken.None);
        }

        // After answer has been sent to the user, wait for their next message and trigger the MessageReceivedAsync method
        private async Task AfterAnswerAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            await context.PostAsync("Is there anything else I can help you with? Click yes or no or simply type another question.");

            // Send a Yes/No choice Hero Card for the user to select an option
            Activity reply = ((Activity)context.Activity).CreateReply();
            HeroCard card = new HeroCard();
            card.Buttons = new List<CardAction>
                            {
                                new CardAction(ActionTypes.ImBack, "Yes", value: "Yes, I require further help."),
                                new CardAction(ActionTypes.ImBack, "No", value: "No, that's all, thanks.")
                            };
            reply.Attachments.Add(card.ToAttachment());
            await context.PostAsync(reply);

            context.Wait(OnOptionSelected);
        }

        // Route the user to appropriate action depending on their choice
        private async Task OnOptionSelected(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            var choice = message.Text;

            if (choice == "Yes, I require further help." || choice.ToLower() == "y" || choice.ToLower() == "yes")
            {
                await context.PostAsync("Sure, please ask another question.");
                context.Wait(MessageReceivedAsync);
            }
            else if (choice == "No, that's all, thanks." || choice.ToLower() == "n" || choice.ToLower() == "no")
            {
                await context.PostAsync("Great, I hope I've managed to help.");

                // Redirect to feedback dialog before the conversation is ended so we can collect user feedback
                context.Call(new FeedbackDialog(), OnFeedbackSubmitted);
            }
            else
            {
                // Assume it's another question so forward on to QnA dialog
                await context.Forward(child: new BasicQnAMakerDialog(), resume: AfterAnswerAsync, item: message, token: CancellationToken.None);
            }
        }

        // Say goodbye and thanks to the user following submission of feedback
        private async Task OnFeedbackSubmitted(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync("Thank you, your feedback will really help me to improve over time. Bye for now!");

            context.EndConversation("");
        }
    }
}