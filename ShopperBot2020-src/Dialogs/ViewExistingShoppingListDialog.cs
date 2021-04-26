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
    public class ViewExistingShoppingListDialog: ComponentDialog
    {
        // A dictionary of existing shopping lists
        protected Dictionary<string, List<ShoppingItem>> _existingShoppingLists;

        private readonly string[] _UserOptions = new string[]
        {
            "Delete Shopping List", "Exit"
        };

        // Constructor: Contains a waterfall sequence for viewing existing shopping lists and maybe deleting one (if the user chooses to do so)
        public ViewExistingShoppingListDialog(Dictionary<string, List<ShoppingItem>> existingShoppingLists) :base(nameof(ViewExistingShoppingListDialog))
        {
            _existingShoppingLists = existingShoppingLists;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog((new ChoicePrompt("MainMenuPrompt") { Style = ListStyle.None }));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                FindListStepAsync,
                ViewShoppingListStepAsync,
                PromptStepAsync,
                UserOptionStepAsync,
                SelectListToDeleteStepAsync,
                DeleteListStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        // This step gives a list of existing shopping lists based on the names the user provided in the process of saving the list
        private async Task<DialogTurnResult> FindListStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if(_existingShoppingLists.Count > 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, here are the current existing shopping lists"));

                // Output the names of the list.
                foreach (string id in _existingShoppingLists.Keys)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{id}"));
                }

                // Ask the user to choose a list to vieww
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please select a shopping list you wish to view. Otherwise, " +
                    "type in 'None'.")
                };

                // Wait for a user input
                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("You don't have any existing shopping lists"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        // This step allows the user to view the shopping list he/she wishes to view.
        private async Task<DialogTurnResult> ViewShoppingListStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            float totalPrice = 0;
            string targetKey = stepContext.Context.Activity.Text;
            List<ShoppingItem> temp;

            // If the user changes his/her mind and doesn't want to view an existing list, the bot sends an acknowledgement message before ending the dialog
            if(targetKey == "None")
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, thanks for your time."));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                // If the user attempts to access a non-existent list, restart the dialog
                if (!_existingShoppingLists.TryGetValue(targetKey, out temp))
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry, but that shopping list doesn't exist"));
                    return await stepContext.ReplaceDialogAsync(nameof(ViewExistingShoppingListDialog), null, cancellationToken);
                }
                else
                {
                    List<ShoppingItem> targetList = temp;
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Alright, here are the items in the shopping list: {targetKey}"));

                    // Reveal every item in the list a user selected for view (assuming the list is valid this time)
                    foreach (ShoppingItem item in targetList)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Item: {item.name}, Quantity: {item.quantity}, Price: {item.price}"));
                        float price = float.Parse(item.price);

                        // Calculate the final price for the shopping list accordingly
                        totalPrice += price;
                    }

                    // Reveal the price of all the items in the list a user wishes to view
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Total Price: {totalPrice}"));
                }

                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        // In this step, the bot prompts the user to decide whether to delete an existing shopping list or end the dialog
        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Text = "Alright, you got to see one of your existing lists. Please select one of the following options:",
                Buttons = _UserOptions.Select(choice => new CardAction(ActionTypes.ImBack, choice, value: choice)).ToList(),
            };

            return await stepContext.PromptAsync("MainMenuPrompt", new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(card.ToAttachment()),
                Choices = ChoiceFactory.ToChoices(_UserOptions),
            }, cancellationToken);
        }

        // In this step, the user makes a decision on whether to delete an existing shopping list or end the dialog
        private async Task<DialogTurnResult> UserOptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string selection = stepContext.Context.Activity.Text;

            // If the user wants to delete a shopping list, then the bot moves on to the dialog step for deleting an existing shopping list
            if (selection == "Delete Shopping List") 
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, thanks for taking the time to review an existing shopping list"));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        // In this step, the bot gives the user lists a user can delete before prompting the user to decide which list to delete.
        // If the user doesn't want to delete a list, all he/she has to do is input 'None'
        private async Task<DialogTurnResult> SelectListToDeleteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, here are the current existing shopping lists"));

            foreach (string id in _existingShoppingLists.Keys)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{id}"));
            }

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please select a shopping list you wish to view. Otherwise, " +
                "type in 'None'.")
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        // The user inputs a name of the list and the bot handles the deletion process accordingly
        private async Task<DialogTurnResult> DeleteListStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string targetKey = stepContext.Context.Activity.Text;
            List<ShoppingItem> temp;
            
            // If the user doesn't want to delete a list, the bot sends an acknowledgement message before ending the dialog
            if (targetKey == "None")
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, thanks for your time. If you want to speak to me again, please leave me " +
                    "a message."));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                // If the user attempts to delete a non-existent list, the bot notfies the user the list doesn't exist before restarting the dialog
                if (!_existingShoppingLists.TryGetValue(targetKey, out temp))
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry, but that shopping list doesn't exist"));
                    return await stepContext.ReplaceDialogAsync(nameof(ViewExistingShoppingListDialog), null, cancellationToken);
                }
                else
                {
                    // The bot deletes the list and displays the remaining ones so the user can verify a successful deletion before closing the dialog.

                    _existingShoppingLists.Remove(targetKey);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, here are the current existing shopping lists. If you don't see any " +
                        "lists, that's because there aren't any saved shopping lists right now."));

                    foreach (string id in _existingShoppingLists.Keys)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{id}"));
                    }
                }

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, thanks for your time. Have a nice day!! Please enter something if you want to " +
                    "talk to me again."));
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}
