﻿using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class InteractiveFlowTests
    {
        private readonly TimeSpan _interactiveAuthTimeout = TimeSpan.FromMinutes(1);
        private static readonly string[] _scopes = new[] { "user.read" };

        #region MSTest Hooks
        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
        }

        #endregion

        [TestMethod]
        public async Task InteractiveAuth_DefaultUserAsync()
        {
            // Arrange
            LabResponse labResponse = LabUserHelper.GetDefaultUser();
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV3_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };


            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV3_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV2,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };


            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV4_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV4_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        private async Task RunTestForUserAsync(LabResponse labResponse)
        {
            PublicClientApplication pca = PublicClientApplicationBuilder.Create(labResponse.AppId)
                                                                        .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                                                                        .BuildConcrete();

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(_scopes, null)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, false))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            IAccount account = await AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 2 - Clear the cache");
            await pca.RemoveAsync(account).ConfigureAwait(false);
            Assert.IsFalse((await pca.GetAccountsAsync().ConfigureAwait(false)).Any());

            Trace.WriteLine("Part 3 - Acquire a token interactively again, with login hint");
            result = await pca
                .AcquireTokenInteractive(_scopes, null)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, true))
                .WithLoginHint(labResponse.User.HomeUPN)
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            account = await AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca.AcquireTokenSilentAsync(_scopes, account).ConfigureAwait(false);
            await AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
        }

        private static SeleniumWebUI CreateSeleniumCustomWebUI(LabUser user, bool withLoginHint)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(user, withLoginHint);
            });
        }

        private static async Task<IAccount> AssertSingleAccountAsync(
            LabResponse labResponse,
            PublicClientApplication pca,
            AuthenticationResult result)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.AccessToken));
            var account = (await pca.GetAccountsAsync().ConfigureAwait(false)).Single();
            Assert.AreEqual(labResponse.User.HomeUPN, account.Username);

            return account;
        }
    }
}
