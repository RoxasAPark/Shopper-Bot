using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples
{
    public class BeginConversationDialog: ComponentDialog
    {
        // Define the command a user would have to give the bot to end the conversation
        private const string EndConversation = "Exit";

        // Define value names for user choice values the dialog tracks via user input
        private const string UserSelection = "value-userChoice";

        // Define the choices a user select when the bot prompts him/her to decide a user option (Create shopping list or end the conversation completely)
        private readonly string[] _UserOptions = new string[]
        {
            "Create Shopping List", "Exit",
        };

        private List<ShoppingItem> itemsList = new List<ShoppingItem>();

        private Dictionary<string, List<ShoppingItem>> existingLists = new Dictionary<string, List<ShoppingItem>>();

        // Constructor defines what kinds of dialogs to use to help the user create a shopping list
        public BeginConversationDialog():base(nameof(BeginConversationDialog))
        {
            AddDialog((new ChoicePrompt("MainMenuPrompt") { Style = ListStyle.None }));

            AddDialog(new CreateShoppingListDialog(itemsList, existingLists));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SelectOptionStepAsync,
                PerformTaskStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        // Function that prompts the user to decide whether to create a shopping list or end the conversation completely 
        private async Task<DialogTurnResult> SelectOptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Text = "Welcome to the Shopper Bot. Please select one of the following options.",
                Buttons = _UserOptions.Select(choice => new CardAction(ActionTypes.ImBack, choice, value: choice)).ToList(),
            };

            return await stepContext.PromptAsync("MainMenuPrompt", new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(card.ToAttachment()),
                Choices = ChoiceFactory.ToChoices(_UserOptions),
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> PerformTaskStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string selection = stepContext.Context.Activity.Text;

            if(selection == "Create Shopping List")
            {
                return await stepContext.BeginDialogAsync(nameof(CreateShoppingListDialog), null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, thanks for chatting with me. Have a nice day!"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}
