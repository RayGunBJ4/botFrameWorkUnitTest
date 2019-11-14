using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VirtualAssistantSample.Dialogs;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    public class CheckoutTests : BotTestBase
    {
        UserState userState;

        [TestMethod]
        public async Task Test_CheckoutWithDiscountDialog()
        {
            userState = new UserState(new MemoryStorage());

            var checkoutDialog = new CheckoutDialog(userState);
            //var mainDialog= new MainDialog(checkoutDialog);
            var testClient = new DialogTestClient(Channels.Msteams, checkoutDialog);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("hi");
            Assert.AreEqual("Do you have membership with us?", ((HeroCard)reply.Attachments[0].Content).Text);

            reply = await testClient.SendActivityAsync<IMessageActivity>("yes");
            Assert.AreEqual("Great! we have special offers for you.", reply.Text);
            reply = testClient.GetNextReply<IMessageActivity>();
            Assert.AreEqual("Do you want to shop and check some offers today?", ((HeroCard)reply.Attachments[0].Content).Text);

            reply = await testClient.SendActivityAsync<IMessageActivity>("Yes");
            Assert.AreEqual("You will receive a coupon shortly.", ((HeroCard)reply.Attachments[0].Content).Text);
        }

        public async Task Test_ValidateCheckoutNoDiscountDialog()
        {
            userState = new UserState(new MemoryStorage());

            var checkoutDialog = new CheckoutDialog(userState);
            //var mainDialog = new MainDialog(checkoutDialog);
            var testClient = new DialogTestClient(Channels.Msteams, checkoutDialog);

            var reply = await testClient.SendActivityAsync<IMessageActivity>("hi");
            Assert.AreEqual("Do you have membership with us?", ((HeroCard)reply.Attachments[0].Content).Text);

            reply = await testClient.SendActivityAsync<IMessageActivity>("no");
            Assert.AreEqual("Do you want to shop and check some offers today?", reply.Text);

            reply = await testClient.SendActivityAsync<IMessageActivity>("Yes");
            Assert.AreEqual("No discount for you at this moment.", ((HeroCard)reply.Attachments[0].Content).Text);
        }


    }
}
