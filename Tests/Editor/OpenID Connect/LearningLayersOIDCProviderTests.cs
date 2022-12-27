﻿using FakeItEasy;
using i5.Toolkit.Core.OpenIDConnectClient;
using i5.Toolkit.Core.TestHelpers;
using i5.Toolkit.Core.Utilities;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace i5.Toolkit.Core.Tests.OpenIDConnectClient
{
    public class LearningLayersOIDCProviderTests
    {
        [Test]
        public void Constructor_Initialized_ContentLoaderNotNull()
        {
            LearningLayersOidcProvider lloidc = new LearningLayersOidcProvider();

            Assert.IsNotNull(lloidc.RestConnector);
        }

        [Test]
        public void Constructor_Initialized_JsonWrapperNotNull()
        {
            LearningLayersOidcProvider lloidc = new LearningLayersOidcProvider();

            Assert.IsNotNull(lloidc.JsonSerializer);
        }

        [UnityTest]
        public IEnumerator GetAccessCodeFromTokenAsync_NoClientData_ReturnsEmptyString()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();

            LogAssert.Expect(LogType.Error, 
                new Regex(@"\w*No client data supplied for the OpenID Connect Client\w*"));

            Task<string> task = lloidc.GetAccessTokenFromCodeAsync("", "");

            yield return AsyncTest.WaitForTask(task);

            string res = task.Result;

            Assert.IsEmpty(res);
        }

        [UnityTest]
        public IEnumerator GetAccessCodeFromTokenAsync_WebResponseSuccess_ReturnsToken()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            A.CallTo(() => lloidc.RestConnector.PostAsync(A<string>.Ignored, A<byte[]>.Ignored, A<Dictionary<string, string>>.Ignored))
                .Returns(Task.FromResult(new WebResponse<string>("json string", null, 200)));
            lloidc.ClientData = A.Fake<ClientData>();
            LearningLayersAuthorizationFlowAnswer answer = new LearningLayersAuthorizationFlowAnswer();
            answer.access_token = "myAccessToken";
            A.CallTo(() => lloidc.JsonSerializer.FromJson<AbstractAuthorizationFlowAnswer>(A<string>.Ignored))
                .Returns(answer);

            Task<string> task = lloidc.GetAccessTokenFromCodeAsync("", "");

            yield return AsyncTest.WaitForTask(task);

            string res = task.Result;

            Assert.AreEqual(answer.access_token, res);
        }

        [UnityTest]
        public IEnumerator GetAccessCodeFromTokenAsync_WebResponseFailed_ReturnsEmptyToken()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            A.CallTo(() => lloidc.RestConnector.PostAsync(A<string>.Ignored, A<byte[]>.Ignored, A<Dictionary<string, string>>.Ignored))
                .Returns(Task.FromResult(new WebResponse<string>("my error", 400)));
            lloidc.ClientData = A.Fake<ClientData>();

            LogAssert.Expect(LogType.Error,
                new Regex(@"\w*my error\w*"));

            Task<string> task = lloidc.GetAccessTokenFromCodeAsync("", "");

            yield return AsyncTest.WaitForTask(task);

            string res = task.Result;

            Assert.IsEmpty(res);
        }

        [Test]
        public void GetAccessToken_TokenProvided_ExtractsToken()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();
            redirectParameters.Add("token", "myAccessToken");

            string res = lloidc.GetAccessToken(redirectParameters);

            Assert.AreEqual("myAccessToken", res);
        }

        [Test]
        public void GetAccessToken_TokenNotProvided_ReturnsEmptyToken()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();

            LogAssert.Expect(LogType.Error, new Regex(@"\w*Redirect parameters did not contain access token\w*"));

            string res = lloidc.GetAccessToken(redirectParameters);

            Assert.IsEmpty(res);
        }

        [UnityTest]
        public IEnumerator GetUserInfoAsync_WebResponseSuccessful_ReturnsUserInfo()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            A.CallTo(() => lloidc.RestConnector.GetAsync(A<string>.Ignored, A<Dictionary<string, string>>.Ignored))
                .Returns(new WebResponse<string>("answer", null, 200));
            LearningLayersUserInfo userInfo = new LearningLayersUserInfo("tester", "tester@test.com", "Tester");
            A.CallTo(() => lloidc.JsonSerializer.FromJson<AbstractUserInfo>(A<string>.Ignored))
                .Returns(userInfo);

            Task<IUserInfo> task = lloidc.GetUserInfoAsync("");

            yield return AsyncTest.WaitForTask(task);

            IUserInfo res = task.Result;

            Assert.AreEqual(userInfo.Email, res.Email);
        }

        [UnityTest]
        public IEnumerator GetUserInfoAsync_WebResponseFailed_ReturnsNull()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            A.CallTo(() => lloidc.RestConnector.GetAsync(A<string>.Ignored, A<Dictionary<string, string>>.Ignored))
                .Returns(new WebResponse<string>("This is a simulated error", 400));

            LogAssert.Expect(LogType.Error, new Regex(@"\w*This is a simulated error\w*"));

            Task<IUserInfo> task = lloidc.GetUserInfoAsync("");

            yield return AsyncTest.WaitForTask(task);

            IUserInfo res = task.Result;

            Assert.IsNull(res);
        }

        [UnityTest]
        public IEnumerator GetUserInfoAsync_JsonParseFailed_ReturnsNull()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            A.CallTo(() => lloidc.RestConnector.GetAsync(A<string>.Ignored, A<Dictionary<string, string>>.Ignored))
                .Returns(new WebResponse<string>("answer", null, 200));
            LearningLayersUserInfo userInfo = new LearningLayersUserInfo("tester", "tester@test.com", "Tester");
            A.CallTo(() => lloidc.JsonSerializer.FromJson<AbstractUserInfo>(A<string>.Ignored))
                .Returns(null);

            LogAssert.Expect(LogType.Error, new Regex(@"\w*Could not parse user info\w*"));

            Task<IUserInfo> task = lloidc.GetUserInfoAsync("");

            yield return AsyncTest.WaitForTask(task);

            IUserInfo res = task.Result;

            Assert.IsNull(res);
        }

        [UnityTest]
        public IEnumerator CheckAccessTokenAsync_WebResponseSuccessful_ReturnsTrue()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            A.CallTo(() => lloidc.RestConnector.GetAsync(A<string>.Ignored, A<Dictionary<string, string>>.Ignored))
                .Returns(new WebResponse<string>("answer", null, 200));
            LearningLayersUserInfo userInfo = new LearningLayersUserInfo("tester", "tester@test.com", "Tester");
            A.CallTo(() => lloidc.JsonSerializer.FromJson<LearningLayersUserInfo>(A<string>.Ignored))
                .Returns(userInfo);

            Task<bool> task = lloidc.CheckAccessTokenAsync("");

            yield return AsyncTest.WaitForTask(task);

            bool res = task.Result;

            Assert.IsTrue(res);
        }

        [UnityTest]
        public IEnumerator CheckAccessTokenAsync_WebResponseFailed_ReturnsFalse()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            A.CallTo(() => lloidc.RestConnector.GetAsync(A<string>.Ignored, A<Dictionary<string,string>>.Ignored))
                .Returns(new WebResponse<string>("This is a simulated error", 400));

            LogAssert.Expect(LogType.Error, new Regex(@"\w*This is a simulated error\w*"));

            Task<bool> task = lloidc.CheckAccessTokenAsync("");

            yield return AsyncTest.WaitForTask(task);

            bool res = task.Result;

            Assert.IsFalse(res);
        }

        [Test]
        public void OpenLoginPage_UriGiven_BrowserOpened()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            lloidc.ClientData = A.Fake<ClientData>();
            string[] testScopes = new string[] { "testScope" };

            lloidc.OpenLoginPage(testScopes, "http://www.test.com");

            A.CallTo(() => lloidc.Browser.OpenURL(A<string>.That.Contains("http://www.test.com"))).MustHaveHappenedOnceExactly();
            A.CallTo(() => lloidc.Browser.OpenURL(A<string>.That.Contains("testScope"))).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void GetAuthorizationCode_CodeProvided_ExtractsCode()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();
            redirectParameters.Add("code", "myCode");
            string res = lloidc.GetAuthorizationCode(redirectParameters);

            Assert.AreEqual("myCode", res);
        }

        [Test]
        public void GetAuthorizationCode_CodeNotProvided_ReturnsEmptyString()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();

            LogAssert.Expect(LogType.Error, new Regex(@"\w*Redirect parameters did not contain authorization code\w*"));

            string res = lloidc.GetAuthorizationCode(redirectParameters);

            Assert.IsEmpty(res);
        }

        [Test]
        public void ParametersContainError_NoError_ReturnsFalse()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();

            bool res = lloidc.ParametersContainError(redirectParameters, out string message);

            Assert.IsFalse(res);
        }

        [Test]
        public void ParametersContainError_NoError_ErrorMessageEmpty()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();

            bool res = lloidc.ParametersContainError(redirectParameters, out string message);

            Assert.IsEmpty(message);
        }

        [Test]
        public void ParametersContainError_ErrorGiven_ReturnsTrue()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();
            redirectParameters.Add("error", "This is a simulated error");

            bool res = lloidc.ParametersContainError(redirectParameters, out string message);

            Assert.IsTrue(res);
        }

        [Test]
        public void ParametersContainError_ErrorGiven_ErrorMessageSet()
        {
            LearningLayersOidcProvider lloidc = CreateProvider();
            Dictionary<string, string> redirectParameters = new Dictionary<string, string>();
            string errorMsg = "This is a simulated error";
            redirectParameters.Add("error", errorMsg);

            bool res = lloidc.ParametersContainError(redirectParameters, out string message);

            Assert.AreEqual(errorMsg, message);
        }

        private LearningLayersOidcProvider CreateProvider()
        {
            LearningLayersOidcProvider lloidc = new LearningLayersOidcProvider();
            lloidc.RestConnector = A.Fake<IRestConnector>();
            lloidc.JsonSerializer = A.Fake<IJsonSerializer>();
            lloidc.Browser = A.Fake<IBrowser>();
            lloidc.GetType().GetField("authorizationEndpoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(lloidc, "https://www.myprovider.com/auth");
            lloidc.GetType().GetField("userInfoEndpoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(lloidc, "https://www.myprovider.com/userInfo");
            lloidc.GetType().GetField("tokenEndpoint", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(lloidc, "https://www.myprovider.com/token");
            return lloidc;
        }
    }
}
