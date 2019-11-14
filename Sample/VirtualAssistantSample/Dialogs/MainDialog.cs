// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Feedback;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Responses.Cancel;
using VirtualAssistantSample.Responses.Main;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private const string Location = "location";
        private const string TimeZone = "timezone";
        private BotSettings _settings;
        private BotServices _services;
        private UserState _userState;
        private MainResponses _responder = new MainResponses();
        private IStatePropertyAccessor<OnboardingState> _onboardingState;
        private IStatePropertyAccessor<SkillContext> _skillContextAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            OnboardingDialog onboardingDialog,
            EscalateDialog escalateDialog,
            CancelDialog cancelDialog,
            CheckoutDialog checkoutDialog,
            List<SkillDialog> skillDialogs,
            IBotTelemetryClient telemetryClient,
            UserState userState)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _userState = userState;
            TelemetryClient = telemetryClient;
            _onboardingState = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            _skillContextAccessor = userState.CreateProperty<SkillContext>(nameof(SkillContext));

            AddDialog(onboardingDialog);
            AddDialog(escalateDialog);
            AddDialog(cancelDialog);
            AddDialog(checkoutDialog);

            foreach (var skillDialog in skillDialogs)
            {
                AddDialog(skillDialog);
            }
        }


        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
                await dc.Context.SendActivityAsync("Welcome!");
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {

            if (dc.Context.Activity.Text.ToLower() == "hi")
            {
                await dc.BeginDialogAsync(nameof(CheckoutDialog));
                //await dc.EndDialogAsync();
            } 
            else
            {
                // If dispatch intent does not map to configured models, send "confused" response.
                // Alternatively as a form of backup you can try QnAMaker for anything not understood by dispatch.
                await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Confused);
            }
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Check if there was an action submitted from intro card
            var result = await dc.ContinueDialogAsync();

            if (result.Status == DialogTurnStatus.Complete)
            {
                await CompleteAsync(dc);
            }

        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The active dialog's stack ended with a complete status
            //await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Completed);

            // Request feedback on the last activity.
            //await FeedbackMiddleware.RequestFeedbackAsync(dc.Context, Id);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {

            return InterruptionAction.NoAction;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            if (dc.ActiveDialog != null && dc.ActiveDialog.Id != nameof(CancelDialog))
            {
                // Don't start restart cancel dialog
                await dc.BeginDialogAsync(nameof(CancelDialog));

                // Signal that the dialog is waiting on user response
                return InterruptionAction.StartedDialog;
            }

            var view = new CancelResponses();
            await view.ReplyWith(dc.Context, CancelResponses.ResponseIds.NothingToCancelMessage);

            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var view = new MainResponses();
            //await view.ReplyWith(dc.Context, MainResponses.ResponseIds.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionAction.MessageSentToUser;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(MainStrings.LOGOUT);

            return InterruptionAction.StartedDialog;
        }

        private class Events
        {
            public const string TimezoneEvent = "VA.Timezone";
            public const string LocationEvent = "VA.Location";
        }
    }
}
