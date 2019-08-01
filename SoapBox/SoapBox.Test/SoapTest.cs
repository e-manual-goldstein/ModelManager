using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoapBox.Test
{
    [TestClass]
    public class SoapTest
    {
        [TestMethod]
        public void Test_HandleNewPushEvent()
        {
            var xmlMessage = File.ReadAllText(@"D:\Goldstein\SoapBox\SoapBox.Test\TestData\SampleSoapMessage.xml");
            var messageService = new MessageService();
            var newEvent = messageService.HandleNewPushEvent(xmlMessage);
            Assert.IsNotNull(newEvent);
        }
    }
}
