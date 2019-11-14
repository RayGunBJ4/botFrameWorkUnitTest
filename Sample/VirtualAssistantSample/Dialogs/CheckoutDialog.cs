using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Dialogs
{
    public class CheckoutDialog : ComponentDialog
    {
        private UserState _userState;

        public CheckoutDialog(UserState userState) : base(nameof(CheckoutDialog))
        {
            InitialDialogId = nameof(CheckoutDialog);

            var checkoutDialog = new WaterfallStep[]
            {
                WelcomeCustomer,
                CheckDiscount,
                AskDiscount,
            };
            _userState = userState;

            AddDialog(new WaterfallDialog(InitialDialogId, checkoutDialog));
            AddDialog(new ConfirmPrompt("welcomeCustomerPrompt"));
            AddDialog(new ConfirmPrompt("continueShoppingPrompt"));
        }

        private async Task<DialogTurnResult> WelcomeCustomer(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                    "welcomeCustomerPrompt",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Do you have membership with us?"),
                    });
        }

        private async Task<DialogTurnResult> CheckDiscount(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {

            var userStateProfileAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateProfileAccessors.GetAsync(stepcontext.Context, () => new UserProfile());

            if ((bool)stepcontext.Result)
            {
                // await stepcontext.context.sendactivityasync("in that case, use \"ibi group geomatics (canada) inc.\".");
                await stepcontext.Context.SendActivityAsync("Great! we have special offers for you.");
                userProfile.DiscountStatus = true;
            }
            else
            {
                userProfile.DiscountStatus = false;
            }
            return await stepcontext.PromptAsync(
                    "continueShoppingPrompt",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Do you want to shop and check some offers today?"),
                    });
        }

        private async Task<DialogTurnResult> AskDiscount(WaterfallStepContext stepcontext, CancellationToken cancellationtoken)
        {

            var userStateProfileAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateProfileAccessors.GetAsync(stepcontext.Context, () => new UserProfile());

            if ((bool)stepcontext.Result)
            {
                if (userProfile.DiscountStatus)
                {
                    await stepcontext.Context.SendActivityAsync("You will receive a coupon shortly.");
                }
                else
                {
                    await stepcontext.Context.SendActivityAsync("No discount for you at this moment.");
                }
            }
            else
            {
                await stepcontext.Context.SendActivityAsync("See you next time.");
            }

            return await stepcontext.EndDialogAsync();
        }

    }
}
