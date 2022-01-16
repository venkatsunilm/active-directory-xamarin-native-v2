﻿using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Helper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace UserDetailsClient
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            CreatePublicClient();
        }

        public static void CreatePublicClient()
        {
            string redirectUri = null;
            switch (Device.RuntimePlatform)
            {
                case Device.Android:
                    redirectUri = App.BrokerRedirectUriOnAndroid;
                    break;
                case Device.iOS:
                    redirectUri = App.BrokerRedirectUriOnIos;
                    break;
            }

            PCAHelper.Init<PCAHelper>(App.ClientID, redirectUri);
            if (Device.RuntimePlatform == Device.UWP)
            {
                PCAHelper.Instance.PCABuilder.WithDefaultRedirectUri();
            }
        }

        private void UpdateUserContent(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                JObject user = JObject.Parse(content);

                Device.BeginInvokeOnMainThread(() =>
                {
                    lblDisplayName.Text = user["displayName"].ToString();
                    lblGivenName.Text = user["givenName"].ToString();
                    lblId.Text = user["id"].ToString();
                    lblSurname.Text = user["surname"].ToString();
                    lblUserPrincipalName.Text = user["userPrincipalName"].ToString();

                    slUser.IsVisible = true;

                    btnSignInSignOut.Text = "Sign out";
                });
            }
        }

        public async Task<string> GetHttpContentWithTokenAsync()
        {
            try
            {
                //get data from API
                HttpClient client = new HttpClient();
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
                PCAHelper.Instance.AddAuthenticationBearerToken(message);
                HttpResponseMessage response = await client.SendAsync(message).ConfigureAwait(false);
                string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return responseString;
            }
            catch (Exception ex)
            {
                await DisplayAlert("API call to graph failed: ", ex.Message, "Dismiss");
                return ex.ToString();
            }
        }

        private async void btnSignInSignOutWithBroker_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (PCAHelper.Instance.AuthResult == null)
                {
                    await PCAHelper.Instance.AcquireTokenAsync(App.Scopes, preferredAccount:(accounts) => accounts.FirstOrDefault()).ConfigureAwait(false);

                    if (PCAHelper.Instance.AuthResult != null)
                    {
                        var content = await GetHttpContentWithTokenAsync().ConfigureAwait(false);
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            UpdateUserContent(content);
                        });
                    }
                }
                else
                {
                    await PCAHelper.Instance.SignOutAsync().ConfigureAwait(false);

                    
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        slUser.IsVisible = false;
                        btnSignInSignOut.Text = "Sign in with Broker";
                    });
                }
            }
            catch (Exception ex)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Authentication failed. See exception message for details: ", ex.Message, "Dismiss").ConfigureAwait(false);
                });
            }
        }
    }
}