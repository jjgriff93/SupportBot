using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System.Net.Http;
using SupportBot.Dialogs;
using System;
using System.Linq;

namespace SupportBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog());
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private async Task HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
            }
            // Greeting message when a new person connects to the bot
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                if (message.MembersAdded.Any(o => o.Id == message.Recipient.Id))
                {
                    var greeting1 = message.CreateReply("Hi, I'm the Dynamics Help Bot for Rank, but my friends call me Hank!");
                    var greeting2 = message.CreateReply("I'm here to help with any questions you have about Dynamics, whether it's managing POs and invoices, how to find certain features, and a lot more. I may not know everything yet but I learn from every interaction.");
                    var greeting3 = message.CreateReply("Try asking me a question.");
                    ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(greeting1);
                    await connector.Conversations.ReplyToActivityAsync(greeting2);
                    await connector.Conversations.ReplyToActivityAsync(greeting3);
                }
            }
            // Skype for Business compatibility: Greeting message when a new person connects to the bot
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                if (message.Action == "add")
                {
                    var greeting1 = message.CreateReply("Hi, I'm the Dynamics Help Bot for Rank, but my friends call me Hank!");
                    var greeting2 = message.CreateReply("I'm here to help with any questions you have about Dynamics, whether it's managing POs and invoices, how to find certain features, and a lot more. I may not know everything yet but I learn from every interaction.");
                    var greeting3 = message.CreateReply("Try asking me a question.");
                    ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(greeting1);
                    await connector.Conversations.ReplyToActivityAsync(greeting2);
                    await connector.Conversations.ReplyToActivityAsync(greeting3);
                }
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
                // Do nothing (testing purposes)
            }
        }
    }
}