// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples
{
    public class ShopperBot<T>: ActivityHandler where T:Dialog
    {
        protected readonly BotState _UserState;
        protected readonly BotState _ConversationState;
        protected readonly Dialog _Dialog;
        protected readonly ILogger _Logger;

        public ShopperBot(ConversationState cs, UserState us, T dialog, ILogger<ShopperBot<T>> logger)
        {
            _ConversationState = cs;
            _UserState = us;
            _Dialog = dialog;
            _Logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            await _ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _Logger.LogInformation("Running message in dialog activity");

            await _Dialog.RunAsync(turnContext, _ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Welcome to the shopper bot. Please type in something to get started.";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
