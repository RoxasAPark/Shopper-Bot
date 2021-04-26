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
    public class CreateShoppingListDialog:ComponentDialog
    {
        // Buttons for the user to decide whether to continue adding items to their shopping list or stop writing the list
        private readonly string[] menuOptions = new string[]
        {
            "Add More Items", "Delete Items", "Done",
        };

        // Buttons for the user to decide whether to view an existing shopping list (View Existing List) or end the conversation (Exit)
        private readonly string[] existingListOptions = new string[]
        {
            "View Existing List", "Exit",
        };

        // List of items a user wishes to purchase later on
        protected List<ShoppingItem> _ShoppingList;

        // A container to hold existing shopping lists
        protected Dictionary<string, List<ShoppingItem>> _createdShoppingLists;

        // A representation of a shopping item a user wishes to add to his/her list
        private const string ShoppingItemToAdd = "value-ShoppingItemToAdd";

        // Constructor: Contains dialogs with steps for creating a list
        // Some steps inclide dialogs for viewing existing lists after a user is finished adding/deleting items

        public CreateShoppingListDialog(List<ShoppingItem> ShoppingList, Dictionary<string, List<ShoppingItem>> createdShoppingLists):base(nameof(CreateShoppingListDialog))
        {
            _ShoppingList = ShoppingList;

            _createdShoppingLists = createdShoppingLists;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog((new ChoicePrompt("MainMenuPrompt") { Style = ListStyle.None }));

            AddDialog(new DeleteItemsDialog(_ShoppingList));
            AddDialog(new ViewExistingShoppingListDialog(_createdShoppingLists));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                NameItemStepAsync,
                NameQuantityStepAsync,
                NamePriceStepAsync,
                AddItemToListStepAsync,
                NextUserActionStepAsync,
                SaveListStepAsync,
                FinalPromptAsync,
                FinalStepAsync,
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        // Bot asks the user which item he/she wishes to add to his/her shopping list
        private async Task<DialogTurnResult> NameItemStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values[ShoppingItemToAdd] = new ShoppingItem();

            // Bot asks for a user input
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Alright, please tell me the name of an item you want to add to your shopping list. Otherwise, type in 'None'.")
            };

            // Bot waits for a user input
            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        // User gives the bot the name of the item. In return, the bot asks for the quantity of the item the user just mentioned.
        private async Task<DialogTurnResult> NameQuantityStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            float totalPrice = 0;
            ShoppingItem Si = (ShoppingItem)stepContext.Values[ShoppingItemToAdd];

            // The user input is the name of the item and gets sent to the bot
            Si.name = (string)stepContext.Result;

            // User input should be 'None' only if the user later decides not to add items to his/her shopping list. 
            if(Si.name == "None")
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, here is your shopping list so far. If you don't see anything below, " +
                    "that's because your list is empty."));

                // Bot shows the user the current shopping list even if he/she decides not to add an item 
                // Case To Handle: What if after adding a few items, a user decides to stop adding items?
                foreach (ShoppingItem item in _ShoppingList)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Item: {item.name}, Quantity: {item.quantity}, Price: {item.price}"));
                    float price = float.Parse(item.price);
                    totalPrice += price;
                }

                // Bot reveals the total cost of all items in the current shopping list
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The total price for your list is: {totalPrice}"));

                return await stepContext.NextAsync(null, cancellationToken);
            }
            else 
            {
                var promptOptions = new PromptOptions
                {
                    // Bot asks for the quantity of the item the user just told the bot to add to his/her shopping list
                    Prompt = MessageFactory.Text($"Alright, please give me a quantity for the item: {Si.name}")
                };

                // Bot waits for a user input
                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Bot asks for the price of the item the user wants to add to his/her shopping list
        private async Task<DialogTurnResult> NamePriceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            ShoppingItem Si = (ShoppingItem)stepContext.Values[ShoppingItemToAdd];

            // If the user doesn't want to add any items, the user shouldn't have to identify a price. 
            if(Si.name == "None")
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                // In this case, the user inputs the quantity of the item he/she wishes to add and sends it to the bot
                Si.quantity = (string)stepContext.Result;

                // In return, the bot asks the user to identify the price of the item based on the quantity. For example, 
                // if the user mentions the quantity of 2 for a specific item, then the user has to inform the price for the quantity of 2.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Alright, please give me the price for the quantity of {Si.quantity} for the item in USD (US Dollars): {Si.name}")
                };

                // Throw exceptions and restart the dialog if the user attempts to add non-integer values for an item quantity.
                // I.e (strings, decimals, characters, etc.)
                try
                {
                    int itemQuantity = Int32.Parse(Si.quantity);

                    // If the user wants to buy an item later, the bot needs to expect a minimum quantity of 1
                    if(itemQuantity < 1)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"If you want to purchase the item, you need a quantity of at least 1"));
                        return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
                    }              
                }
                catch (FormatException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input an integer. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
                catch (OverflowException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input an integer. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
                catch (ArgumentNullException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input an integer. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
                catch (ArgumentException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input an integer. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
            }

        }

        // User gives the bot the price of the item he/she wishes to add. In return, the bot reveals the current list of items and the total price for the entire list
        private async Task<DialogTurnResult> AddItemToListStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            ShoppingItem Si = (ShoppingItem)stepContext.Values[ShoppingItemToAdd];

            if(Si.name == "None")
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                float totalPrice = 0;
                // User inputs the price of the item based on the quantity and sends it to the bot
                Si.price = (string)stepContext.Result;

                // If the user inputs invalid prices (strings, characters, etc.), the program throws an exception and restarts the dialog
                try
                {
                    float itemPrice = float.Parse(Si.price);

                    // A user should not be able to enter negative numbers for a price for an item. 
                    if(itemPrice < 0)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You can't have a price less than 0."));
                        return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                    }
                    else
                    {
                        // Item inserted into the list
                        _ShoppingList.Add(Si);

                        // Bot shows the entire list and updates the total price accordingly (not shown to the user while outputting each item because the total price is the last step)
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, here is your shopping list so far"));

                        foreach (ShoppingItem item in _ShoppingList)
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Item: {item.name}, Quantity: {item.quantity}, Price: {item.price}"));
                            float price = float.Parse(item.price);
                            totalPrice += price;
                        }

                        // Finally, the bot reveals the total price for all of the items in the list
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The total price for your list is: {totalPrice}"));
                        return await stepContext.NextAsync(null, cancellationToken);
                    }              
                }
                catch (FormatException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input a price in USD Dollars. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
                catch (OverflowException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input a price in USD Dollars. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
                catch (ArgumentNullException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input a price in USD Dollars. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
                catch (ArgumentException)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"You're supposed to input a price in USD Dollars. Since your input is invalid, the item " +
                        $"{Si.name} will not be added to your shopping list"));
                    return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
                }
            }
        }

        // The bot gives the user the choice of adding more items, remove unwanted items, or finalize the list
        private async Task<DialogTurnResult> NextUserActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Text = "Please select one of the following options.",
                Buttons = menuOptions.Select(choice => new CardAction(ActionTypes.ImBack, choice, value: choice)).ToList(),
            };

            return await stepContext.PromptAsync("MainMenuPrompt", new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(card.ToAttachment()),
                Choices = ChoiceFactory.ToChoices(menuOptions),
            }, cancellationToken);
        }

        // The user clicks a button and can either add more items to the list, remove unwanted items, or finalize the list
        private async Task<DialogTurnResult> SaveListStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string selection = stepContext.Context.Activity.Text;

            if (selection == "Add More Items")
            {
                return await stepContext.ReplaceDialogAsync(nameof(CreateShoppingListDialog), menuOptions, cancellationToken);
            }
            else if (selection == "Delete Items")
            {
                return await stepContext.ReplaceDialogAsync(nameof(DeleteItemsDialog), menuOptions, cancellationToken);
            }
            else
            {
                // Only save the list if it's not empty (obviously, containing at least one item).
                if(_ShoppingList.Count > 0)
                {
                    var promptOptions = new PromptOptions
                    {
                        // Bot asks for the user to provide a name for the list he/she just created
                        Prompt = MessageFactory.Text($"Okay, before you save the list, please provide a name for it.")
                    };

                    // Bot waits for a user input
                    return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(nameof(TextPrompt), cancellationToken);
                }
            }
        }

        // In this step, the bot saves the list after the user creates a name. Then the bot gives the user the option of viewing existing lists
        // Or ending the conversation.
        private async Task<DialogTurnResult> FinalPromptAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if(_ShoppingList.Count > 0)
            {
                string key = (string)stepContext.Result;
                List<ShoppingItem> temp = new List<ShoppingItem>();

                foreach (ShoppingItem item in _ShoppingList)
                {
                    temp.Add(item);
                }

                // Save the list for later view
                _createdShoppingLists.Add(key, temp);
                _ShoppingList.Clear();

                // Bot prompts the user to decide whether to view existing lists or end the conversation.
                var card = new HeroCard
                {
                    Text = "Please select one of the following options.",
                    Buttons = existingListOptions.Select(choice => new CardAction(ActionTypes.ImBack, choice, value: choice)).ToList(),
                };

                return await stepContext.PromptAsync("MainMenuPrompt", new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(card.ToAttachment()),
                    Choices = ChoiceFactory.ToChoices(existingListOptions),
                }, cancellationToken);
            }
            else
            {
                var card = new HeroCard
                {
                    Text = "Please select one of the following options.",
                    Buttons = existingListOptions.Select(choice => new CardAction(ActionTypes.ImBack, choice, value: choice)).ToList(),
                };

                return await stepContext.PromptAsync("MainMenuPrompt", new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(card.ToAttachment()),
                    Choices = ChoiceFactory.ToChoices(existingListOptions),
                }, cancellationToken);
            }
        }

        // In this step, the user either views an existing shopping list or ends the conversation
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string selection = stepContext.Context.Activity.Text;

            // If the user decides to view an existing list, the bot runs a separate dialog for viewing existing lists. 
            if (selection == "View Existing List")
            {
                return await stepContext.ReplaceDialogAsync(nameof(ViewExistingShoppingListDialog), menuOptions, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Alright, thanks for taking the time to create and save your shopping list. " +
                    "If you want to talk to me again, please type in something below."));

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}
