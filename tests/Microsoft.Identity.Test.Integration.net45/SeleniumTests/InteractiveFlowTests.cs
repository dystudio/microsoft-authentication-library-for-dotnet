//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Microsoft.Identity.Client;
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
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Test.Unit;
using System.Globalization;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;


namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class InteractiveFlowTests
    {
        private readonly TimeSpan _interactiveAuthTimeout = TimeSpan.FromMinutes(1);
        private static readonly string[] _scopes = new[] { "user.read" };
        private TokenCache cache;

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
        public async Task Interactive_AADAsync()
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

        [TestMethod]
        public async Task Interactive_AdfsV2019_NotFederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2019_FederatedAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Interactive_AdfsV2019_DirectAsync()
        {
            // Arrange
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);
            await RunTestForUserAsync(labResponse, true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task MultiUserCacheCompatabilityTestAsync()
        {
            // Arrange
            cache = new TokenCache();

            LabResponse labResponseDefault = LabUserHelper.GetDefaultUser();
            AuthenticationResult defaultAccountResult = await RunTestForUserAsync(labResponseDefault).ConfigureAwait(false);

            UserQuery FederatedUserquery = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponseFederated = LabUserHelper.GetLabUserData(FederatedUserquery);
            var federatedAccountResult = await RunTestForUserAsync(labResponseFederated, true).ConfigureAwait(false);

            UserQuery MSAUserquery = new UserQuery
            {
                UserSearch = LabApiConstants.MSAOutlookAccount,
                IsExternalUser = true,
                AppName = "Lab4V2App"
            };

            LabResponse labResponseMsa = LabUserHelper.GetLabUserData(MSAUserquery);
            labResponseMsa.AppId = LabApiConstants.MSAOutlookAccountClientID;
            var msaAccountResult = await RunTestForUserAsync(labResponseMsa).ConfigureAwait(false);

            PublicClientApplication pca = PublicClientApplicationBuilder.Create(labResponseDefault.AppId).BuildConcrete();
            pca.UserTokenCacheInternal = cache;

            AuthenticationResult authResult = await pca.AcquireTokenSilentAsync(new[] { CoreUiTestConstants.DefaultScope }, defaultAccountResult.Account).ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);

            pca = PublicClientApplicationBuilder.Create(labResponseFederated.AppId).BuildConcrete();
            pca.UserTokenCacheInternal = cache;

            authResult = await pca.AcquireTokenSilentAsync(new[] { CoreUiTestConstants.DefaultScope },
                   federatedAccountResult.Account).ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);

            pca = PublicClientApplicationBuilder.Create(LabApiConstants.MSAOutlookAccountClientID).BuildConcrete();
            pca.UserTokenCacheInternal = cache;

            authResult = await pca.AcquireTokenSilentAsync(new[] { CoreUiTestConstants.DefaultScope }, msaAccountResult.Account).ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNull(authResult.IdToken);

            cache = null;
        }

        private async Task<AuthenticationResult> RunTestForUserAsync(LabResponse labResponse, bool directToAdfs = false)
        {
            PublicClientApplication pca;
            if(directToAdfs)
            {
                pca = PublicClientApplicationBuilder.Create(Adfs2019LabConstants.PublicClientId)
                                                    .WithRedirectUri(Adfs2019LabConstants.ClientRedirectUri)
                                                    .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                                                    .BuildConcrete();
            }
            else
            {
                pca = PublicClientApplicationBuilder.Create(labResponse.AppId)
                                                    .WithRedirectUri(SeleniumWebUI.FindFreeLocalhostRedirectUri())
                                                    .BuildConcrete();
            }

            Trace.WriteLine("Part 1 - Acquire a token interactively, no login hint");
            AuthenticationResult result = await pca
                .AcquireTokenInteractive(_scopes, null)
                .WithCustomWebUi(CreateSeleniumCustomWebUI(labResponse.User, false))
                .ExecuteAsync(new CancellationTokenSource(_interactiveAuthTimeout).Token)
                .ConfigureAwait(false);
            IAccount account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

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
            account = await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);

            Trace.WriteLine("Part 4 - Acquire a token silently");
            result = await pca.AcquireTokenSilentAsync(_scopes, account).ConfigureAwait(false);
            await MsalAssert.AssertSingleAccountAsync(labResponse, pca, result).ConfigureAwait(false);
            return result;
        }

        private static SeleniumWebUI CreateSeleniumCustomWebUI(LabUser user, bool withLoginHint)
        {
            return new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(user, withLoginHint);
            });
        }    
    }
}
