﻿using FakeItEasy;
using i5.Toolkit.Core.AppConsole;
using i5.Toolkit.Core.Editor.TestHelpers;
using NUnit.Framework;

namespace i5.Toolkit.Core.Tests.AppConsole
{
    public class DefaultConsoleFormatterLogicTests
    {
        [SetUp]
        public void SetUp()
        {
            EditModeTestUtilities.ResetScene();
        }

        [Test]
        public void Format_OutputIsLogContent()
        {
            const string expectedContent = "my test content";
            DefaultConsoleFormatterLogic formatterLogic = new DefaultConsoleFormatterLogic();

            ILogMessage logMessage = A.Fake<ILogMessage>();
            A.CallTo(() => logMessage.Content).Returns(expectedContent);

            string result = formatterLogic.Format(logMessage);

            Assert.AreEqual(expectedContent, result);
        }
    }
}
