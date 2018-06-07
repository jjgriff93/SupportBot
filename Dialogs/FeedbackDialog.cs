using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SupportBot.Dialogs
{
    [Serializable]
    public class FeedbackDialog : IDialog<object>
    {
        private Guid userSessionReference;
        
        // Ask the user if they'd like to give feedback
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Just before you go, if you've got time, could you rate how you thought I did? This will really help the team to improve me over time.");

            userSessionReference = new Guid();
            
            // Send a feedback Hero Card for the user to select an option
            Activity reply = ((Activity)context.Activity).CreateReply();
            HeroCard card = new HeroCard();
            card.Buttons = new List<CardAction>
                            {
                                new CardAction(ActionTypes.ImBack, "Excellent: answered all queries", value: "Excellent"),
                                new CardAction(ActionTypes.ImBack, "Good: answered most queries", value: "Good"),
                                new CardAction(ActionTypes.ImBack, "Poor: took a while to get answers", value: "Poor"),
                                new CardAction(ActionTypes.ImBack, "Very poor: got no answers at all", value: "Very Poor")
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

            switch (choice)
            {
                case "Excellent":
                    await context.PostAsync("Thank you. I'm glad to hear that!");
                    break;
                case "Good":
                    await context.PostAsync("Thank you. I'm glad to hear that!");
                    break;
                case "Poor":
                    await context.PostAsync("Thank you. Sorry I wasn't able to be more helpful.");
                    break;
                case "Very Poor":
                    await context.PostAsync("Thank you. Sorry I wasn't able to be more helpful.");
                    break;
                default:
                    await context.PostAsync("Sorry, I didn't understand your choice. Please click one of the buttons.");
                    context.Wait(OnOptionSelected);
                    break;
            }

            // Log the verb feedback to a Logic App now in case they leave chat
            await PostToFeedbackLogicApp(userSessionReference, choice, null);

            await context.PostAsync("If you'd like to leave a couple of lines of written feedback to help me continue to improve, please send me a message now and I'll pass it on to the team. Otherwise, have a great day!");

            // Await written comments
            context.Wait(OnFeedbackSubmitted);
        }

        private async Task OnFeedbackSubmitted(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            var feedbackMessage = message.Text;

            // Log the feedback verb alongside the feedback comments
            await PostToFeedbackLogicApp(userSessionReference, null, feedbackMessage);

            context.Done("");
        }

        private async Task PostToFeedbackLogicApp(Guid userSessionReference, string feedbackVerb, string feedbackMessage)
        {
            // Log user's feedback verb
            Debug.WriteLine("Feedback Verb: " + feedbackVerb);
            // If they also provided verbatim, log this as well
            if (feedbackMessage != null)
            {
                Debug.WriteLine("Feedback Message: " + feedbackMessage);
            }

            // Post to a Logic App to aggregate the bot feedback and integrate into dashboards etc.
            var HTTPClient = new HttpClient();
            var HTTPRequest = new HttpRequestMessage()
            {
                //Logic App Uri populated by config injection
                RequestUri = new Uri(ConfigurationManager.AppSettings["FeedbackLogicAppTriggerURI"]),
                Method = HttpMethod.Post
            };
            string HTTPContentString = "";
            // Log just the verb if no message is provided
            if (feedbackMessage != null)
            {
                HTTPContentString = "{\"userSessionReference\":\"" + userSessionReference.ToString() + "\", \"feedbackVerb\":\"\", \"feedbackMessage\":\"" + feedbackMessage + "\"}";
            }
            else
            {
                HTTPContentString = "{\"userSessionReference\":\"" + userSessionReference.ToString() + "\", \"feedbackVerb\":\"" + feedbackVerb + "\", \"feedbackMessage\":\"\"}";
            }
            HTTPRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HTTPRequest.Content = new StringContent(HTTPContentString, Encoding.UTF8, "application/json");
            await HTTPClient.SendAsync(HTTPRequest);
        }
    }
}