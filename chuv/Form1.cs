using System;
using System.Diagnostics;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace chuv
{
    public partial class Form1 : Form
    {
        private ChromiumWebBrowser chromiumWebBrowser1;

        public Form1()
        {
            InitializeComponent();
            InitializeChromium();
            this.Load += MainForm_Load;
        }

        private void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            settings.CefCommandLineArgs.Add("disable-web-security", "2");
            settings.CefCommandLineArgs.Add("allow-running-insecure-content", "1");
            Cef.Initialize(settings);

            chromiumWebBrowser1 = new ChromiumWebBrowser("about:blank") // Start with a blank page
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(chromiumWebBrowser1);
            chromiumWebBrowser1.FrameLoadEnd += ChromeBrowser_FrameLoadEnd;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ShowCloudSelectionDialog();
        }

        private void ShowCloudSelectionDialog()
        {
            var cloudSelectionForm = new CloudSelectionForm();
            if (cloudSelectionForm.ShowDialog() == DialogResult.OK)
            {
                switch (cloudSelectionForm.SelectedCloud.ToLower()) // Convert to lower case for case-insensitive comparison
                {
                    case "yandex":
                        StartAuthenticationInDefaultBrowser("https://oauth.yandex.ru/authorize", "bef6f48146c44b4683199cf3d31370a2", "https://disk.yandex.ru/client/disk");
                        break;
                    case "dropbox":
                        StartAuthenticationInChromium("https://www.dropbox.com/oauth2/authorize", "yxogmfag6j750ug", "https://www.dropbox.com/home");
                        break;
                    case "bybit":
                        StartAuthenticationInChromium("https://www.bybit.com/login", "uzIHXn2KqHxwN7CYMp", "https://www.bybit.com");
                        break;
                }
            }
        }

        private void StartAuthenticationInChromium(string authUrl, string clientId, string redirectUri)
        {
            string url = $"{authUrl}?response_type=token&client_id={clientId}&redirect_uri={redirectUri}";
            chromiumWebBrowser1.Load(url);
        }

        private void StartAuthenticationInDefaultBrowser(string authUrl, string clientId, string redirectUri)
        {
            string url = $"{authUrl}?response_type=token&client_id={clientId}&redirect_uri={redirectUri}";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private void ChromeBrowser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Url.Contains("access_token="))
            {
                string token = ExtractAccessToken(e.Url);
                if (!string.IsNullOrEmpty(token))
                {
                    string apiUrl = e.Url.Contains("yandex") ? $"https://cloud-api.yandex.net/v1/disk/resources/files?oauth_token={token}"
                                  : e.Url.Contains("dropbox") ? $"https://api.dropboxapi.com/2/files/list_folder?access_token={token}"
                                  : e.Url.Contains("bybit") ? $"https://api.bybit.com?access_token={token}"
                                  : null;

                    if (!string.IsNullOrEmpty(apiUrl))
                    {
                        chromiumWebBrowser1.Load(apiUrl);
                    }
                }
            }
        }

        private string ExtractAccessToken(string url)
        {
            var tokenIndex = url.IndexOf("access_token=");
            return tokenIndex != -1 ? url.Substring(tokenIndex + "access_token=".Length).Split('&')[0] : string.Empty;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Cef.Shutdown();
            base.OnFormClosing(e);
        }
    }
}
