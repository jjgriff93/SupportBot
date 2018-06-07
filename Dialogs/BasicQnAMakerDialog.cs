using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Connector;
using System.Configuration;
using System.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;

namespace SupportBot
{
    // Dialog for QnAMaker GA service
    [Serializable]
    public class BasicQnAMakerDialog : QnAMakerDialog
    {
        public BasicQnAMakerDialog() : base(new QnAMakerService(new QnAMakerAttribute(ConfigurationManager.AppSettings["QnAAuthKey"], ConfigurationManager.AppSettings["QnAKnowledgebaseId"], "Sorry, I'm not sure about that. Try asking in a different way.", 0.35, 3, ConfigurationManager.AppSettings["QnAEndpointHostName"])))
        { }

        // Override for confidence between 0.75 and 0.35 - return multiple options for the user to choose from
        protected override async Task QnAFeedbackStepAsync(IDialogContext context, QnAMakerResults qnaMakerResults)
        {
            // responding with the top answer when score is above some threshold
            if (qnaMakerResults.Answers.Count > 0 && qnaMakerResults.Answers.FirstOrDefault().Score > 0.7)
            {
                await context.PostAsync(qnaMakerResults.Answers.First().Answer);
            }
            else 
            {
                await base.QnAFeedbackStepAsync(context, qnaMakerResults);
            }
        }

        /*protected override async Task RespondFromQnAMakerResultAsync(IDialogContext context, IMessageActivity message, QnAMakerResults qnaMakerResults)
        {
            // Use this override to change how a response from QnA Maker is formatted (useful for Hero Cards for example) - leaving it as is for now
        }*/

        // Override to log all of the user's messages and catch any unanswered questions - flag these
        protected override async Task DefaultWaitNextMessageAsync(IDialogContext context, IMessageActivity message, QnAMakerResults qnaMakerResults)
        {
            // Log user's question and QnA confidence in it's reply
            Debug.WriteLine("User's question: " + message.Text + " | Confidence score: " + qnaMakerResults.Answers.FirstOrDefault().Score);

            // Post to a Logic App if confidence is low or no answers are returned from QnA
            if (qnaMakerResults.Answers.Count == 0 || qnaMakerResults.Answers.FirstOrDefault().Score <= 0.35)
            {
                var HTTPClient = new HttpClient();
                var HTTPRequest = new HttpRequestMessage()
                {
                    //Logic App Uri populated by config injection
                    RequestUri = new Uri(ConfigurationManager.AppSettings["MissingResponseLogicAppTriggerURI"]),
                    Method = HttpMethod.Post
                };
                string HTTPContentString = "{\"reponseNotFoundFor\":\"" + message.Text + "\"}";
                HTTPRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HTTPRequest.Content = new StringContent(HTTPContentString, Encoding.UTF8, "application/json");
                await HTTPClient.SendAsync(HTTPRequest);

                await base.DefaultWaitNextMessageAsync(context, message, qnaMakerResults);
            }
            else
            {
                await base.DefaultWaitNextMessageAsync(context, message, qnaMakerResults);
            }
        }
    }
}