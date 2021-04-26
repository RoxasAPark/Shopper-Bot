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
    public class DeleteItemsDialog:ComponentDialog
    {
        protected List<ShoppingItem> _ShoppingList;

        // Define the choices a user select when the bot prompts him/her to decide a user option (Create shopping list or end the conversation completely)
        private readonly string[] _UserOptions = new string[]
        {
            "Delete More Items", "Exit"
        };

        public DeleteItemsDialog(List<ShoppingItem> shoppingList):base(nameof(DeleteItemsDialog))
        {
            _ShoppingList = shoppingList;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog((new ChoicePrompt("MainMenuPrompt") { Style = ListStyle.None }));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
           {
                FindItemStepAsync,
                RemoveItemStepAsync,
                FinalStepAsync,
           }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        // Bot prompts the user to identify the name of the item a user wishes to remove from his/her shopping list. Note, this is case sensitive.
        private async Task<DialogTurnResult> FindItemStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if(_ShoppingList.Count > 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, here is your current shopping list so far. Please select one of the items below"));

                foreach (ShoppingItem item in _ShoppingList)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Item: {item.name}, Quantity: {item.quantity}, Price: {item.price}"));
                }

                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Alright, please tell me the name of an item you want to remove from your shopping list. Otherwise, " +
                    "type in 'None'.")
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your shopping list is empty. Therefore, there are no items to delete from this list"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        // Bot searches for the item and removes it from the list and updates the price before revealing the updated shopping list to the user
        private async Task<DialogTurnResult> RemoveItemStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string targetItem = stepContext.Context.Activity.Text;

            // If the unwanted item exists in the list
            if(_ShoppingList.Exists(item => item.name == targetItem))
            {
                float totalPrice = 0;

                // Remove the item
                ShoppingItem temp = _ShoppingList.Find(item => item.name.Contains(targetItem));
                _ShoppingList.Remove(temp);

                // Bot reveals the updated list
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, here is your updated shopping list so far. Please write it down somewhere."));

                foreach (ShoppingItem item in _ShoppingList)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Item: {item.name}, Quantity: {item.quantity}, Price: {item.price}"));

                    // Bot updates the price
                    float price = float.Parse(item.price);
                    totalPrice += price;
                }
                
                // Bot reveals the total price after updating the list
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Total Price: {totalPrice}"));

                // Bot prompts the user to decide if he/she wants to remove more items or not
                var card = new HeroCard
                {
                    Text = "You deleted an item from your shopping list. Please select one of the options below.",
                    Buttons = _UserOptions.Select(choice => new CardAction(ActionTypes.ImBack, choice, value: choice)).ToList(),
                };

                // Bot awaits a selection
                return await stepContext.PromptAsync("MainMenuPrompt", new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(card.ToAttachment()),
                    Choices = ChoiceFactory.ToChoices(_UserOptions),
                }, cancellationToken);
            }
            else if(targetItem == "None") // User doesn't want to remove items from the list
            {
                // Return to the dialog for adding items
                return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), _UserOptions, cancellationToken);
            }
            else // Item doesn't exist
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry, that item doesn't exist in your current shopping list"));
                return await stepContext.ReplaceDialogAsync(nameof(DeleteItemsDialog), null, cancellationToken);
            }
        }

        // User makes a decision on whether to delete more items or exit the dialog
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string selection = stepContext.Context.Activity.Text;

            // If the user wants to delete more items
            if(selection == "Delete More Items") // The user and the bot repeat this dialog to complete the process of deleting another item
            {
                return await stepContext.ReplaceDialogAsync(nameof(DeleteItemsDialog), _UserOptions, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), _UserOptions, cancellationToken);
            }
        }
    }
}
