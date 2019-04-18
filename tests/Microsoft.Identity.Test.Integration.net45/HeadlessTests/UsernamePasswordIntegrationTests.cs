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
#if !WINDOWS_APP && !ANDROID && !iOS // U/P not available on UWP, Android and iOS
using System.Globalization;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Note: these tests require permission to a KeyVault Microsoft account; 
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class UsernamePasswordIntegrationTests
    {
        private const string _authority = "https://login.microsoftonline.com/organizations/";
        private static readonly string[] _scopes = { "User.Read" };

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

        #region Happy Path Tests
        [TestMethod]
        public async Task ROPC_AAD_Async()
        {
            var labResponse = LabUserHelper.GetDefaultUser();
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ROPC_ADFSv4Federated_Async()
        {
            var labResponse = LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4, true);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ROPC_ADFSv4Managed_Async()
        {
            var labResponse = LabUserHelper.GetAdfsUser(FederationProvider.AdfsV4, false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ROPC_ADFSv3Federated_Async()
        {
            var labResponse = LabUserHelper.GetAdfsUser(FederationProvider.AdfsV3, true);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ROPC_ADFSv3Managed_Async()
        {
            var labResponse = LabUserHelper.GetAdfsUser(FederationProvider.AdfsV3, false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

		[TestMethod]
        public async Task ROPC_ADFSv2Fderated_Async()
        {
            var labResponse = LabUserHelper.GetAdfsUser(FederationProvider.AdfsV2, true);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory("UsernamePasswordIntegrationTests")]
        public async Task AcquireTokenFromAdfsUsernamePasswordAsync()
        {
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.ADFSv2019,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = true
            };

            LabResponse labResponse = LabUserHelper.GetLabUserData(query);


            var user = labResponse.User;

            SecureString securePassword = new NetworkCredential("", LabUserHelper.GetUserPassword(user)).SecurePassword;

            PublicClientApplication msalPublicClient = new PublicClientApplication(Adfs2019LabConstants.PublicClientId, Adfs2019LabConstants.Authority);
            AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(_scopes, user.Upn, securePassword).ConfigureAwait(false);
            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
        }

        #endregion

        [TestMethod]
        public async Task AcquireTokenWithManagedUsernameIncorrectPasswordAsync()
        {
            var labResponse = LabUserHelper.GetDefaultUser();
            var user = labResponse.User;

            SecureString incorrectSecurePassword = new SecureString();
            incorrectSecurePassword.AppendChar('x');
            incorrectSecurePassword.MakeReadOnly();

            PublicClientApplication msalPublicClient = new PublicClientApplication(labResponse.AppId, _authority);

            try
            {
                var result =
                     await msalPublicClient.AcquireTokenByUsernamePasswordAsync(_scopes, user.Upn, incorrectSecurePassword)
                     .ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(ex.CorrelationId));
                Assert.AreEqual(400, ex.StatusCode);
                Assert.AreEqual("invalid_grant", ex.ErrorCode);
                Assert.IsTrue(ex.Message.StartsWith("AADSTS50126: Invalid username or password"));

                return;
            }

            Assert.Fail("Bad exception or no exception thrown");
        }

        [TestMethod]
        public void AcquireTokenWithFederatedUsernameIncorrectPassword()
        {
            UserQuery query = new UserQuery
            {
                FederationProvider = FederationProvider.AdfsV4,
                IsMamUser = false,
                IsMfaUser = false,
                IsFederatedUser = false
            };

            var labResponse = LabUserHelper.GetLabUserData(query);
            var user = labResponse.User;

            SecureString incorrectSecurePassword = new SecureString();
            incorrectSecurePassword.AppendChar('x');
            incorrectSecurePassword.MakeReadOnly();

            PublicClientApplication msalPublicClient = new PublicClientApplication(labResponse.AppId, _authority);

            var result = Assert.ThrowsExceptionAsync<MsalException>(async () =>
                 await msalPublicClient.AcquireTokenByUsernamePasswordAsync(_scopes, user.Upn, incorrectSecurePassword).ConfigureAwait(false));
        }

        private async Task RunHappyPathTestAsync(LabResponse labResponse)
        {
            var user = labResponse.User;

            SecureString securePassword = new NetworkCredential("", user.Password).SecurePassword;

            PublicClientApplication msalPublicClient = new PublicClientApplication(labResponse.AppId, _authority);

            //AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(Scopes, user.Upn, securePassword).ConfigureAwait(false);
            AuthenticationResult authResult = await msalPublicClient.AcquireTokenByUsernamePasswordAsync(_scopes, user.Upn, securePassword).ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.AreEqual(user.Upn, authResult.Account.Username);
            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following: 
            // 1) Add in code to pull the user's password before creating the SecureString, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }

    }
}
#endif
