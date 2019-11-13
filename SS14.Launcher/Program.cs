using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Content.Client.UserInterface;
using Content.Client.Utility;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Lite;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Control;

namespace SS14.Launcher
{
    internal class Program
    {
        private const string JenkinsBaseUrl = "https://builds.spacestation14.io/jenkins";
        private const string JenkinsJobName = "SS14 Content";
        private const string CurrentLauncherVersion = "1";

        private readonly HttpClient _httpClient;
        private string _dataDir;

        private LauncherInterface _interface;
        private string ClientBin => Path.Combine(_dataDir, "client_bin");

#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _loc;
        [Dependency] private readonly ITaskManager _taskManager;
        [Dependency] private readonly IUriOpener _uriOpener;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
        [Dependency] private readonly IGameController _gameController;
#pragma warning restore 649

        public static void Main()
        {
            FixTlsVersions();

            LiteLoader.Run(() => new Program().Run(), new InitialWindowParameters
            {
                Size = (500, 250),
                WindowTitle = "SS14 Launcher"
            });
        }

        private Program()
        {
            IoCManager.InjectDependencies(this);

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"SS14.Launcher v{CurrentLauncherVersion}");
        }

        private async void Run()
        {
            _dataDir = Path.Combine(UserDataDir.GetUserDataDir(), "launcher");

            _userInterfaceManager.Stylesheet = new NanoStyle().Stylesheet;

            _interface = new LauncherInterface
            {
                ProgressBarVisible = false
            };

            _interface.StatusLabel.Text = _loc.GetString("Checking for launcher update..");
            _userInterfaceManager.StateRoot.AddChild(_interface.RootControl);

            try
            {
                var needsUpdate = await NeedLauncherUpdate();
                if (needsUpdate)
                {
                    _interface.StatusLabel.Text = _loc.GetString("This launcher is out of date.");
                    _interface.LaunchButton.Text = _loc.GetString("Download update.");
                    _interface.LaunchButton.OnPressed +=
                        _ => _uriOpener.OpenUri("https://spacestation14.io/about/nightlies/");
                    _interface.LaunchButton.Disabled = false;
                    return;
                }

                await RunUpdate();

                _interface.StatusLabel.Text = _loc.GetString("Ready!");
                _interface.LaunchButton.Disabled = false;
                _interface.LaunchButton.OnPressed += _ =>
                {
                    LaunchClient();
                    _gameController.Shutdown();
                };
            }
            catch (Exception e)
            {
                Logger.ErrorS("launcher", "Exception while trying to run updates:\n{0}", e);
                _interface.ProgressBarVisible = false;
                _interface.StatusLabel.Text =
                    _loc.GetString("An error occured.\nMake sure you can access builds.spacestation14.io");
            }
        }

        private async Task<bool> NeedLauncherUpdate()
        {
            var launcherVersionUri =
                new Uri($"{JenkinsBaseUrl}/userContent/current_launcher_version.txt");
            var versionRequest = await _httpClient.GetAsync(launcherVersionUri);
            versionRequest.EnsureSuccessStatusCode();
            return CurrentLauncherVersion != (await versionRequest.Content.ReadAsStringAsync()).Trim();
        }

        private async Task RunUpdate()
        {
            _interface.StatusLabel.Text = _loc.GetString("Checking for client update..");

            Logger.InfoS("launcher", "Checking for update...");

            var jobUri = new Uri($"{JenkinsBaseUrl}/job/{Uri.EscapeUriString(JenkinsJobName)}/api/json");
            var jobDataResponse = await _httpClient.GetAsync(jobUri);
            if (!jobDataResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Got bad status code {jobDataResponse.StatusCode} from Jenkins.");
            }

            var jobInfo = JsonConvert.DeserializeObject<JenkinsJobInfo>(
                await jobDataResponse.Content.ReadAsStringAsync());
            var latestBuildNumber = jobInfo.LastSuccessfulBuild.Number;

            var versionFile = Path.Combine(_dataDir, "current_build");
            bool needUpdate;
            if (File.Exists(versionFile))
            {
                var buildNumber = int.Parse(File.ReadAllText(versionFile, EncodingHelpers.UTF8),
                    CultureInfo.InvariantCulture);
                needUpdate = buildNumber != latestBuildNumber;
                if (needUpdate)
                {
                    Logger.InfoS("launcher", "Current version ({0}) is out of date, updating to {1}.", buildNumber,
                        latestBuildNumber);
                }
            }
            else
            {
                Logger.InfoS("launcher", "As it turns out, we don't have any version yet. Time to update.");
                // Version file doesn't exist, assume first run or whatever.
                needUpdate = true;
            }

            if (!needUpdate)
            {
                Logger.InfoS("launcher", "No update needed!");
                return;
            }

            _interface.StatusLabel.Text = _loc.GetString("Downloading client update..");
            _interface.ProgressBarVisible = true;
            var binPath = Path.Combine(_dataDir, "client_bin");

            await Task.Run(() =>
            {
                if (!Directory.Exists(binPath))
                {
                    Directory.CreateDirectory(binPath);
                }
                else
                {
                    Helpers.ClearDirectory(binPath);
                }
            });

            // We download the artifact to a temporary file on disk.
            // This is to avoid having to load the entire thing into memory.
            // (.NET's zip code loads it into a memory stream if the stream you give it doesn't support seeking.)
            // (this makes a lot of sense due to how the zip file format works.)
            var tmpFile = await _downloadArtifactToTempFile(latestBuildNumber, GetBuildFilename());
            _interface.StatusLabel.Text = _loc.GetString("Extracting update..");
            _interface.ProgressBarVisible = false;

            Logger.InfoS("launcher", "Extracting: '{0}' to '{1}'", tmpFile, binPath);
            await Task.Run(() =>
            {
                using (var file = File.OpenRead(tmpFile))
                {
                    Helpers.ExtractZipToDirectory(binPath, file);
                }

                File.Delete(tmpFile);
            });

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // .NET's zip extraction system doesn't seem to preserve +x.
                // Technically can't blame it because there's no "official" way to store that,
                // since zip files are DOS-centric.

                // Manually chmod +x the App bundle then.
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x '{Path.Combine("Space Station 14.app", "Contents", "MacOS", "SS14")}'",
                    WorkingDirectory = binPath,
                });
                process?.WaitForExit();
            }

            // Write version to disk.
            File.WriteAllText(versionFile, latestBuildNumber.ToString(CultureInfo.InvariantCulture),
                EncodingHelpers.UTF8);

            Logger.InfoS("launcher", "Update done!");
        }

        private async Task<string> _downloadArtifactToTempFile(int buildNumber, string fileName)
        {
            var artifactUri
                = new Uri(
                    $"{JenkinsBaseUrl}/job/{Uri.EscapeUriString(JenkinsJobName)}/{buildNumber}/artifact/release/{Uri.EscapeUriString(fileName)}");

            var tmpFile = Path.GetTempFileName();
            Logger.InfoS("launcher", "temp download file path: {0}", tmpFile);
            await _httpClient.DownloadToFile(artifactUri, tmpFile, f => _taskManager.RunOnMainThread(() =>
            {
                _interface.ProgressBarVisible = true;
                _interface.ProgressBar.Value = f;
            }));

            return tmpFile;
        }

        private void LaunchClient()
        {
            var binPath = ClientBin;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "mono",
                        Arguments = "Robust.Client.exe",
                        WorkingDirectory = binPath,
                        UseShellExecute = false,
                    },
                };
                process.Start();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(binPath, "Robust.Client.exe"),
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // TODO: does this cause macOS to make a security warning?
                // If it does we'll have to manually launch the contents, which is simple enough.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = "'Space Station 14.app'",
                    WorkingDirectory = binPath,
                });
            }
            else
            {
                throw new NotSupportedException("Unsupported platform.");
            }
        }


        [Pure]
        private static string GetBuildFilename()
        {
            string platform;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = "Windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = "Linux";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "macOS";
            }
            else
            {
                throw new NotSupportedException("Unsupported platform.");
            }

            return $"SS14.Client_{platform}_x64.zip";
        }

        [Conditional("NETFRAMEWORK")]
        private static void FixTlsVersions()
        {
            // So, supposedly .NET Framework 4.7 is supposed to automatically select sane TLS versions.
            // Yet, it does not for some people. This causes it to try to connect to our servers with
            // SSL 3 or TLS 1.0, neither of which are accepted for security reasons.
            // (The minimum our servers accept is TLS 1.2)
            // So, ONLY on Windows (Mono is fine) and .NET Framework we manually tell it to use TLS 1.2
            // I assume .NET Core does not have this issue being disconnected from the OS and all that.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
        }

        private class LauncherInterface
        {
#pragma warning disable 649
            [Dependency] private readonly IResourceCache _resourceCache;
            [Dependency] private readonly ILocalizationManager _loc;
            [Dependency] private readonly IUriOpener _uriOpener;
#pragma warning restore 649

            public Control RootControl { get; }
            public Label StatusLabel { get; }
            public ProgressBar ProgressBar { get; }
            public Button LaunchButton { get; }

            public bool ProgressBarVisible
            {
                set
                {
                    ProgressBar.Visible = value;
                    StatusLabel.SizeFlagsHorizontal = value ? SizeFlags.Fill : SizeFlags.FillExpand;
                }
            }

            public LauncherInterface()
            {
                IoCManager.InjectDependencies(this);

                Button visitWebsiteButton;

                RootControl = new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat
                    {
                        BackgroundColor = Color.FromHex("#20202a"),
                        ContentMarginLeftOverride = 4,
                        ContentMarginRightOverride = 4,
                        ContentMarginBottomOverride = 4,
                        ContentMarginTopOverride = 4
                    },
                    Children =
                    {
                        new VBoxContainer
                        {
                            Children =
                            {
                                new Label
                                {
                                    Text = _loc.GetString("Space Station 14"),
                                    FontOverride = _resourceCache.GetFont("/Fonts/Animal Silence.otf", 40),
                                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                                },

                                (visitWebsiteButton = new Button
                                {
                                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                                    SizeFlagsVertical = SizeFlags.Expand | SizeFlags.ShrinkCenter,
                                    Text = _loc.GetString("Visit website")
                                }),

                                new HBoxContainer
                                {
                                    SizeFlagsVertical = SizeFlags.ShrinkEnd,
                                    SeparationOverride = 5,
                                    Children =
                                    {
                                        (StatusLabel = new Label()),
                                        (ProgressBar = new ProgressBar
                                        {
                                            SizeFlagsHorizontal = SizeFlags.FillExpand,
                                            MinValue = 0,
                                            MaxValue = 1
                                        }),
                                        (LaunchButton = new Button
                                        {
                                            Disabled = true,
                                            Text = _loc.GetString("Launch!")
                                        })
                                    }
                                }
                            }
                        }
                    }
                };

                visitWebsiteButton.OnPressed += _ => _uriOpener.OpenUri("https://spacestation14.io");

                RootControl.SetAnchorPreset(LayoutPreset.Wide);
            }
        }
    }
}
