using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Line = System.Windows.Shapes.Line;
using Polyline = System.Windows.Shapes.Polyline;

[assembly: AssemblyTitle("MyVibe")]
[assembly: AssemblyDescription("A focused manager for installing and maintaining Adobe plug-ins.")]
[assembly: AssemblyCompany("MyVibe")]
[assembly: AssemblyProduct("MyVibe")]
[assembly: AssemblyCopyright("Copyright 2026 MyVibe")]
[assembly: AssemblyVersion("0.4.0.0")]
[assembly: AssemblyFileVersion("0.4.0.0")]
[assembly: AssemblyInformationalVersion("0.4.0")]

namespace MyVibe
{
    internal static class Program
    {
        internal static string PendingHealthPath;

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 2 && String.Equals(args[0], "--replace-manager", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = ManagerUpdates.ReplaceFromRequest(args[1]) ? 0 : 1;
                return;
            }

            if (args.Length == 2 && String.Equals(args[0], "--update-health", StringComparison.OrdinalIgnoreCase))
                PendingHealthPath = args[1];

            if (args.Length == 6 && String.Equals(args[0], "--elevated-install-package", StringComparison.OrdinalIgnoreCase))
            {
                PluginDefinition plugin = PluginRegistry.Find(args[2]);
                if (!PluginInstaller.IsAdministrator() || plugin == null || !PluginInstaller.IsSafeResultPath(args[1]) || !CatalogClient.IsSafeDownloadPath(args[3]))
                {
                    Environment.ExitCode = 5;
                    return;
                }
                OperationResult packageResult = PluginInstaller.InstallPackage(plugin, args[3], args[4], args[5]);
                File.WriteAllText(args[1], (packageResult.Success ? "OK\n" : "ERROR\n") + packageResult.Message);
                Environment.ExitCode = packageResult.Success ? 0 : 1;
                return;
            }

            if (args.Length == 3 && String.Equals(args[0], "--elevated-update-packages", StringComparison.OrdinalIgnoreCase))
            {
                if (!PluginInstaller.IsAdministrator() || !PluginInstaller.IsSafeResultPath(args[1]) || !PluginInstaller.IsSafeBatchRequestPath(args[2]))
                {
                    Environment.ExitCode = 5;
                    return;
                }
                OperationResult batchResult = PluginInstaller.InstallPackagesFromRequest(args[2]);
                File.WriteAllText(args[1], (batchResult.Success ? "OK\n" : "ERROR\n") + batchResult.Message);
                Environment.ExitCode = batchResult.Success ? 0 : 1;
                return;
            }

            if (args.Length == 3 && (String.Equals(args[0], "--elevated-install", StringComparison.OrdinalIgnoreCase) || String.Equals(args[0], "--elevated-remove", StringComparison.OrdinalIgnoreCase)))
            {
                PluginDefinition plugin = PluginRegistry.Find(args[2]);
                if (!PluginInstaller.IsAdministrator() || plugin == null || !PluginInstaller.IsSafeResultPath(args[1]))
                {
                    Environment.ExitCode = 5;
                    return;
                }
                OperationResult result = String.Equals(args[0], "--elevated-install", StringComparison.OrdinalIgnoreCase)
                    ? PluginInstaller.Install(plugin)
                    : PluginInstaller.Remove(plugin);
                File.WriteAllText(args[1], (result.Success ? "OK\n" : "ERROR\n") + result.Message);
                Environment.ExitCode = result.Success ? 0 : 1;
                return;
            }

            if (args.Length > 0 && String.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
            {
                string error;
                bool pathsAreSafe = PluginInstaller.IsSafeResultPath(Path.Combine(PluginInstaller.IpcRoot, "result-self-test.txt"))
                    && !PluginInstaller.IsSafeResultPath(Path.Combine(Path.GetTempPath(), "myvibe-unsafe.txt"));
                bool registryIsValid = PluginRegistry.All.Length == 2
                    && PluginRegistry.Find("com.badru.transform2d5") != null
                    && PluginRegistry.Find("com.badru.tonemesh") != null;
                bool releaseUrlsAreStrict = CatalogClient.IsTrustedReleaseUrl("https://github.com/badrulmokhtar/myvibe/releases/download/test/file.zip")
                    && !CatalogClient.IsTrustedReleaseUrl("https://github.com/attacker/myvibe/releases/download/test/file.zip")
                    && !CatalogClient.IsTrustedReleaseUrl("http://github.com/badrulmokhtar/myvibe/releases/download/test/file.zip");
                bool archivePathsAreSafe = ManagerUpdates.IsSafeArchiveEntryName("folder/MyVibe.exe")
                    && !ManagerUpdates.IsSafeArchiveEntryName("../MyVibe.exe")
                    && !ManagerUpdates.IsSafeArchiveEntryName("/MyVibe.exe");
                bool manualVersionDetectionWorks = String.Equals(
                        PluginInstaller.ParseCepManifestVersionForTest("<ExtensionManifest ExtensionBundleVersion=\"0.8.4\" />"),
                        "0.8.4",
                        StringComparison.Ordinal)
                    && PluginInstaller.ParseCepManifestVersionForTest("<!DOCTYPE x [<!ENTITY y SYSTEM \"file:///windows/win.ini\">]><ExtensionManifest ExtensionBundleVersion=\"9.9.9\">&y;</ExtensionManifest>") == null;
                bool catalogSignatureIsValid = CatalogClient.VerifyCatalogSignature(
                    System.Text.Encoding.UTF8.GetBytes("MyVibe catalog signature self-test"),
                    System.Text.Encoding.ASCII.GetBytes("tTc4JE4UZfJEFKdu0BY80q0MwDuTYG0K1BPnybwExKhK1G0xy4l0tpFd+rcBmu/s4UFAm6082lEz7S45y7N7w3eTSCEJpNzALsX2zL8vXM5HUUJn8rascoxsKWSmQUsw6UCQvVi5WT6mcJn1WKD0jHmD7lrX09t5m10YE40P7AV0SEdQrsuTPwtiPmB0IRwyvUHGclZ3MVv7EcCCLjIrS2aEo9oJ+oy/c+TUWGv0BQhqPY8BYIlOnGmbkM2bxBaqe3nzYTyEGR0E0n8+/nb2y6yNVvkg614o/67Kt/C6gfDTM/MliNGnFfDYIyR6F73THBo7n05pr1hTeV0DbChZg1lAiGvcLLylchUjzJiVJuKdv7F28sY1xklsDT2AJkdU77nXxBua8dHblT3/1i/oQwWNKjmsNxjvfl9bGUCFbv72SF+g+dMiiGeoTY/e8qF/PEgx46YvrVttfx1q64Kg/5ezyKWz5wdqYJjY9JHNkwaKJoFJVqrhbxE5xLVj6X1U"));
                bool catalogV2SignatureIsValid = CatalogClient.VerifyCatalogV2Signature(
                    System.Text.Encoding.UTF8.GetBytes("MyVibe catalog v2 signature self-test"),
                    System.Text.Encoding.ASCII.GetBytes("qJT2aY5GS/t73NnN/OuEg6S/eYuFWFxHv3qsTsv2V5RdWc+6gAgzXrJJG75rK11GUeXZLvDV6ugL92UE4sdnfEhpzrWsKF0WFW00XbTA31HeM4RUv15k1dJwtrVbxVmL8XMfCne3U3ddAP8gGqze8b+LqZ+0Z0MBomtgm3izWHsFpmYzQHEkZuQ0alYEc4zI5LC3EYH5F9teNXbBf4ozrfHXnq9MY5dBYLb8ppTvsoAGY2QKmRmWufoQfGyn9CQeQrNwEWzS0lOcgXRI8MvtVwvmQs/3akpV/qJ08klqTo8bjqE1fOg8R059DIZRuVlxttKT4epO6ubfPJ0b74Je7+TqyW5eq+PE+HP8yxQlvgQ9x/zcDuDv2PE79r1OZORevxdg22utOeb3g2ShvPvsqxjScXk22SUM2NcCaBqLmJE8JKgnoMuyOjokcTKf/h/o3HgXY1PPgnrqiHMyXIf0LYMX7gdYSSB9tMV1lAzKFZRSJz3OB9IcX6OqpHI+nD18"));
                bool valid = PluginInstaller.ValidatePayload(PluginRegistry.Transform2D5, out error)
                    && pathsAreSafe && registryIsValid && releaseUrlsAreStrict && archivePathsAreSafe && manualVersionDetectionWorks && catalogSignatureIsValid && catalogV2SignatureIsValid;
                if (!pathsAreSafe)
                    error = "administrator result-path validation failed";
                else if (!registryIsValid)
                    error = "plugin registry validation failed";
                else if (!releaseUrlsAreStrict)
                    error = "release URL allowlist validation failed";
                else if (!archivePathsAreSafe)
                    error = "manager archive path validation failed";
                else if (!manualVersionDetectionWorks)
                    error = "manual plugin version detection failed";
                else if (!catalogSignatureIsValid)
                    error = "catalog v1 signature validation failed";
                else if (!catalogV2SignatureIsValid)
                    error = "catalog v2 signature validation failed";
                Console.WriteLine(valid ? "MyVibe self-test passed." : "MyVibe self-test failed: " + error);
                Environment.ExitCode = valid ? 0 : 1;
                return;
            }

            if (args.Length == 3 && String.Equals(args[0], "--verify-catalog", StringComparison.OrdinalIgnoreCase))
            {
                string error;
                bool valid = CatalogClient.VerifyCatalogFiles(args[1], args[2], out error);
                Console.WriteLine(valid ? "MyVibe catalog verification passed." : "MyVibe catalog verification failed: " + error);
                Environment.ExitCode = valid ? 0 : 1;
                return;
            }

            if (args.Length > 1 && String.Equals(args[0], "--screenshot", StringComparison.OrdinalIgnoreCase))
            {
                Application previewApp = new Application();
                ManagerWindow preview = new ManagerWindow();
                if (args.Length > 4)
                    preview.SelectPreviewPlugin(args[4]);
                int previewWidth;
                int previewHeight;
                if (args.Length > 3 && Int32.TryParse(args[2], out previewWidth) && Int32.TryParse(args[3], out previewHeight))
                {
                    preview.Width = Math.Max(preview.MinWidth, previewWidth);
                    preview.Height = Math.Max(preview.MinHeight, previewHeight);
                }
                preview.Show();
                preview.UpdateLayout();
                RenderTargetBitmap bitmap = new RenderTargetBitmap(
                    (int)preview.ActualWidth,
                    (int)preview.ActualHeight,
                    96,
                    96,
                    PixelFormats.Pbgra32);
                bitmap.Render(preview);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (FileStream output = File.Create(args[1]))
                    encoder.Save(output);
                preview.Close();
                previewApp.Shutdown();
                return;
            }

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnMainWindowClose;
                app.Run(new ManagerWindow());
            }
            catch (Exception ex)
            {
                string report = Path.Combine(Path.GetTempPath(), "MyVibe-startup-error.log");
                File.WriteAllText(report, ex.ToString());
                MessageBox.Show("MyVibe could not start. A diagnostic report was saved to:\n\n" + report, "MyVibe", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    internal enum InstallState
    {
        Available,
        Installed,
        Incomplete
    }

    internal sealed class PluginDefinition
    {
        internal readonly string Id;
        internal readonly string Name;
        internal readonly string Version;
        internal readonly string Summary;
        internal readonly string CardSummary;
        internal readonly string AipFileName;
        internal readonly string CepFolderName;
        internal readonly string StateFileName;
        internal readonly string CachePattern;
        internal readonly string PayloadResource;
        internal readonly bool IsToneMesh;

        internal PluginDefinition(string id, string name, string version, string summary, string cardSummary,
            string aipFileName, string cepFolderName, string stateFileName, string cachePattern, string payloadResource, bool isToneMesh)
        {
            Id = id;
            Name = name;
            Version = version;
            Summary = summary;
            CardSummary = cardSummary;
            AipFileName = aipFileName;
            CepFolderName = cepFolderName;
            StateFileName = stateFileName;
            CachePattern = cachePattern;
            PayloadResource = payloadResource;
            IsToneMesh = isToneMesh;
        }
    }

    internal static class PluginRegistry
    {
        internal static readonly PluginDefinition Transform2D5 = new PluginDefinition(
            "com.badru.transform2d5", "2.5D Transform", "0.9.5",
            "Create precise 2.5D views while keeping text, vectors, gradients, and linked artwork editable.",
            "Perspective transformation for editable Illustrator artwork, with linked views and reusable presets.",
            "2.5D Transform.aip", "2.5D Transform", "transform2d5.version", "ILST_*_com.badru.transform2d5.*", "MyVibe.Transform2D5.zip", false);

        internal static readonly PluginDefinition ToneMesh = new PluginDefinition(
            "com.badru.tonemesh", "ToneMesh", "0.9.8",
            "Build editable halftone fields from solid fills, gradients, mesh gradients, and custom marks.",
            "Editable tone-driven vector fields with custom structures, marks, appearance handles, and contour wrapping.",
            "ToneMesh.aip", "ToneMesh", "tonemesh.version", "ILST_*_com.badru.tonemesh.*", null, true);

        internal static readonly PluginDefinition[] All = { Transform2D5, ToneMesh };

        internal static PluginDefinition Find(string id)
        {
            return All.FirstOrDefault(delegate(PluginDefinition plugin) { return String.Equals(plugin.Id, id, StringComparison.OrdinalIgnoreCase); });
        }
    }

    internal sealed class PluginCardBindings
    {
        internal Button Card;
        internal TextBlock State;
        internal Border Pill;
    }

    internal sealed class ManagerWindow : Window
    {
        private static readonly SolidColorBrush AppBackground = Brush(21, 23, 26);
        private static readonly SolidColorBrush Surface = Brush(29, 32, 36);
        private static readonly SolidColorBrush Raised = Brush(36, 40, 45);
        private static readonly SolidColorBrush Hover = Brush(42, 47, 53);
        private static readonly SolidColorBrush Structure = Brush(52, 58, 66);
        private static readonly SolidColorBrush Text = Brush(244, 246, 248);
        private static readonly SolidColorBrush Muted = Brush(169, 176, 185);
        private static readonly SolidColorBrush Subtle = Brush(119, 127, 137);
        private static readonly SolidColorBrush Accent = Brush(75, 156, 255);
        private static readonly SolidColorBrush AccentHover = Brush(103, 173, 255);
        private static readonly SolidColorBrush AccentPressed = Brush(45, 127, 215);
        private static readonly SolidColorBrush Success = Brush(99, 201, 149);
        private static readonly SolidColorBrush Warning = Brush(240, 180, 92);
        private static readonly SolidColorBrush Danger = Brush(255, 145, 137);

        private readonly Grid _workspace;
        private readonly Border _libraryPane;
        private readonly Border _detailPane;
        private readonly TextBlock _libraryCount;
        private readonly Dictionary<string, PluginCardBindings> _cards = new Dictionary<string, PluginCardBindings>(StringComparer.OrdinalIgnoreCase);
        private readonly TextBlock _detailState;
        private readonly Border _detailStatePill;
        private readonly Button _primaryButton;
        private readonly ProgressBar _progress;
        private readonly TextBlock _operationMessage;
        private readonly Button _checkUpdateButton;
        private readonly Button _updateAllButton;
        private readonly TextBlock _managerUpdateStatus;
        private bool _busy;
        private readonly Dictionary<string, CatalogPackage> _catalogPackages = new Dictionary<string, CatalogPackage>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CatalogPackage> _availablePluginUpdates = new Dictionary<string, CatalogPackage>(StringComparer.OrdinalIgnoreCase);
        private CatalogPackage _availableManagerUpdate;
        private PluginDefinition _selectedPlugin = PluginRegistry.Transform2D5;
        private Border _detailIcon;
        private TextBlock _detailName;
        private TextBlock _detailVersion;
        private TextBlock _detailSummary;
        private TextBlock _aipPathValue;
        private TextBlock _cepPathValue;

        internal ManagerWindow()
        {
            Title = "MyVibe";
            Width = 1080;
            Height = 720;
            MinWidth = 640;
            MinHeight = 540;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = AppBackground;
            Foreground = Text;
            FontFamily = new FontFamily("Segoe UI Variable, Segoe UI");
            FontSize = 13;

            DockPanel root = new DockPanel();
            Content = root;

            Border header = BuildHeader();
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            Border footer = BuildFooter();
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            _workspace = new Grid();
            _workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(410) });
            _workspace.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _workspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.Children.Add(_workspace);

            TextBlock count;
            _libraryPane = BuildLibraryPane(out count);
            _libraryCount = count;
            Grid.SetColumn(_libraryPane, 0);
            _workspace.Children.Add(_libraryPane);

            TextBlock detailState;
            Border detailPill;
            Button primary;
            ProgressBar progress;
            TextBlock operationMessage;
            Button checkUpdate;
            Button updateAll;
            TextBlock updateStatus;
            _detailPane = BuildDetailPane(out detailState, out detailPill, out primary, out progress, out operationMessage, out checkUpdate, out updateAll, out updateStatus);
            _detailState = detailState;
            _detailStatePill = detailPill;
            _primaryButton = primary;
            _progress = progress;
            _operationMessage = operationMessage;
            _checkUpdateButton = checkUpdate;
            _updateAllButton = updateAll;
            _managerUpdateStatus = updateStatus;
            Grid.SetColumn(_detailPane, 1);
            _workspace.Children.Add(_detailPane);

            _primaryButton.Click += PrimaryActionClick;
            _checkUpdateButton.Click += CheckForUpdatesClick;
            _updateAllButton.Click += UpdateAllPluginsClick;
            SelectPlugin(_selectedPlugin, false);
            SizeChanged += delegate { UpdateResponsiveLayout(); };
            Loaded += delegate
            {
                RefreshState();
                if (!String.IsNullOrEmpty(Program.PendingHealthPath))
                    ManagerUpdates.MarkHealthy(Program.PendingHealthPath);
            };
        }

        internal void SelectPreviewPlugin(string pluginId)
        {
            PluginDefinition plugin = PluginRegistry.Find(pluginId);
            if (plugin != null)
                SelectPlugin(plugin, false);
        }

        private Border BuildHeader()
        {
            Grid headerGrid = new Grid { Margin = new Thickness(24, 16, 24, 16) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border mark = new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(9),
                Background = Raised,
                BorderBrush = Structure,
                BorderThickness = new Thickness(1),
                Child = BuildMyVibeMark(22)
            };
            headerGrid.Children.Add(mark);

            StackPanel brand = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            brand.Children.Add(new TextBlock { Text = "MyVibe", Foreground = Text, FontSize = 17, FontWeight = FontWeights.SemiBold });
            brand.Children.Add(new TextBlock { Text = "Plugin manager", Foreground = Subtle, FontSize = 11 });
            Grid.SetColumn(brand, 1);
            headerGrid.Children.Add(brand);

            Border version = new Border
            {
                Background = Surface,
                BorderBrush = Structure,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6, 10, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = "MyVibe 0.4.0", Foreground = Muted, FontSize = 11 }
            };
            Grid.SetColumn(version, 2);
            headerGrid.Children.Add(version);

            return new Border { Background = Brush(16, 18, 21), BorderBrush = Structure, BorderThickness = new Thickness(0, 0, 0, 1), Child = headerGrid };
        }

        private Border BuildFooter()
        {
            DockPanel footer = new DockPanel { Margin = new Thickness(24, 10, 24, 10), LastChildFill = true };
            TextBlock security = new TextBlock
            {
                Text = "Downloads are verified before MyVibe changes Illustrator.",
                Foreground = Subtle,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            footer.Children.Add(security);

            TextBlock elevation = new TextBlock
            {
                Text = "Administrator access requested only for changes",
                Foreground = Muted,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(elevation, Dock.Right);
            footer.Children.Add(elevation);

            return new Border { Background = Brush(16, 18, 21), BorderBrush = Structure, BorderThickness = new Thickness(0, 1, 0, 0), Child = footer };
        }

        private Border BuildLibraryPane(out TextBlock count)
        {
            StackPanel content = new StackPanel { Margin = new Thickness(32, 30, 32, 32) };
            content.Children.Add(new TextBlock { Text = "Plugins", Foreground = Text, FontSize = 24, FontWeight = FontWeights.SemiBold });
            count = new TextBlock { Text = "2 available", Foreground = Muted, Margin = new Thickness(0, 4, 0, 24) };
            content.Children.Add(count);
            foreach (PluginDefinition plugin in PluginRegistry.All)
                content.Children.Add(BuildPluginCard(plugin));

            Border note = new Border
            {
                Background = Surface,
                BorderBrush = Structure,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 22, 0, 0)
            };
            StackPanel noteContent = new StackPanel();
            noteContent.Children.Add(new TextBlock { Text = "Designed for a clean installation", Foreground = Text, FontWeight = FontWeights.SemiBold });
            noteContent.Children.Add(new TextBlock
            {
                Text = "MyVibe backs up the current plug-in before replacing or removing it, and stops if Illustrator is open.",
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 0),
                LineHeight = 19
            });
            note.Child = noteContent;
            content.Children.Add(note);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = content };
            return new Border { Background = AppBackground, Child = scroll };
        }

        private Button BuildPluginCard(PluginDefinition plugin)
        {
            Button card = new Button
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 12),
                Background = Surface,
                BorderBrush = plugin == _selectedPlugin ? Accent : Structure,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Template = BuildButtonTemplate(Surface, Hover, Raised, new CornerRadius(12)),
                FocusVisualStyle = BuildFocusStyle(new CornerRadius(12))
            };
            AutomationProperties.SetName(card, "View details for " + plugin.Name);
            card.Click += delegate { SelectPlugin(plugin, true); };

            Grid cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardGrid.Children.Add(new Border
            {
                Width = 66,
                Height = 66,
                Background = Raised,
                BorderBrush = Structure,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = BuildPluginMark(plugin, 38),
                VerticalAlignment = VerticalAlignment.Top
            });

            StackPanel info = new StackPanel();
            Grid identity = new Grid();
            identity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            identity.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            identity.Children.Add(new TextBlock { Text = plugin.Name, Foreground = Text, FontSize = 18, FontWeight = FontWeights.SemiBold });
            TextBlock state = new TextBlock { Text = "Available", Foreground = Accent, FontSize = 11, FontWeight = FontWeights.SemiBold };
            Border pill = new Border
            {
                Background = WithAlpha(Accent, 28),
                BorderBrush = WithAlpha(Accent, 90),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Child = state
            };
            Grid.SetColumn(pill, 1);
            identity.Children.Add(pill);
            info.Children.Add(identity);
            info.Children.Add(new TextBlock
            {
                Text = plugin.CardSummary,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 8, 0, 14)
            });
            WrapPanel metadata = new WrapPanel();
            metadata.Children.Add(MetaChip("Version " + plugin.Version));
            metadata.Children.Add(MetaChip("Illustrator 2026"));
            metadata.Children.Add(MetaChip("Windows x64"));
            info.Children.Add(metadata);
            Grid.SetColumn(info, 1);
            cardGrid.Children.Add(info);
            card.Content = cardGrid;
            _cards[plugin.Id] = new PluginCardBindings { Card = card, State = state, Pill = pill };
            return card;
        }

        private Border BuildDetailPane(
            out TextBlock detailState,
            out Border detailPill,
            out Button primary,
            out ProgressBar progress,
            out TextBlock operationMessage,
            out Button checkUpdate,
            out Button updateAll,
            out TextBlock managerUpdateStatus)
        {
            StackPanel content = new StackPanel { Margin = new Thickness(28, 28, 28, 36) };
            content.Children.Add(new TextBlock { Text = "Plugin details", Foreground = Subtle, FontSize = 11, FontWeight = FontWeights.SemiBold });

            Grid identity = new Grid { Margin = new Thickness(0, 18, 0, 16) };
            identity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            identity.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _detailIcon = new Border
            {
                Width = 48,
                Height = 48,
                Background = Raised,
                BorderBrush = Structure,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = BuildPluginMark(_selectedPlugin, 28)
            };
            identity.Children.Add(_detailIcon);
            StackPanel titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _detailName = new TextBlock { Text = _selectedPlugin.Name, Foreground = Text, FontSize = 20, FontWeight = FontWeights.SemiBold };
            _detailVersion = new TextBlock { Text = "Version " + _selectedPlugin.Version, Foreground = Subtle, FontSize = 11 };
            titles.Children.Add(_detailName);
            titles.Children.Add(_detailVersion);
            Grid.SetColumn(titles, 1);
            identity.Children.Add(titles);
            content.Children.Add(identity);

            detailState = new TextBlock { Text = "Available to install", Foreground = Accent, FontSize = 11, FontWeight = FontWeights.SemiBold };
            detailPill = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = WithAlpha(Accent, 28),
                BorderBrush = WithAlpha(Accent, 90),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Child = detailState
            };
            content.Children.Add(detailPill);

            _detailSummary = new TextBlock
            {
                Text = _selectedPlugin.Summary,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 14, 0, 18)
            };
            content.Children.Add(_detailSummary);

            primary = CreateButton("Install plugin", true);
            primary.Height = 42;
            primary.HorizontalAlignment = HorizontalAlignment.Stretch;
            AutomationProperties.SetName(primary, "Install " + _selectedPlugin.Name);
            content.Children.Add(primary);

            progress = new ProgressBar
            {
                Height = 4,
                IsIndeterminate = true,
                Foreground = Accent,
                Background = Raised,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 10, 0, 0)
            };
            content.Children.Add(progress);

            operationMessage = new TextBlock
            {
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(0, 9, 0, 0),
                MinHeight = 18
            };
            AutomationProperties.SetLiveSetting(operationMessage, AutomationLiveSetting.Polite);
            content.Children.Add(operationMessage);

            content.Children.Add(SectionTitle("Compatibility"));
            content.Children.Add(MetaRow("Application", PluginInstaller.IsSupportedIllustratorInstalled() ? "Illustrator 2026 detected" : "Illustrator 2026 required"));
            content.Children.Add(MetaRow("Operating system", "Windows 10/11"));
            content.Children.Add(MetaRow("Architecture", "x64"));

            content.Children.Add(SectionTitle("Included components"));
            content.Children.Add(ComponentRow("Native .aip engine", "Required"));
            content.Children.Add(ComponentRow("CEP interface", "Required"));
            bool runtimeAvailable = PluginInstaller.HasNativeRuntime();
            content.Children.Add(ComponentRow("Visual C++ runtime", runtimeAvailable ? "Detected" : "Missing", runtimeAvailable ? Success : Danger));

            content.Children.Add(SectionTitle("Install locations"));
            _aipPathValue = new TextBlock { Text = PluginInstaller.GetAipPath(_selectedPlugin), Foreground = Muted, FontSize = 10, TextWrapping = TextWrapping.Wrap };
            _cepPathValue = new TextBlock { Text = PluginInstaller.GetCepPath(_selectedPlugin), Foreground = Muted, FontSize = 10, TextWrapping = TextWrapping.Wrap };
            content.Children.Add(PathText(_aipPathValue));
            content.Children.Add(PathText(_cepPathValue));

            content.Children.Add(SectionTitle("MyVibe updates"));
            managerUpdateStatus = new TextBlock
            {
                Text = "Manager updates are checked separately from plugin installation.",
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 10)
            };
            content.Children.Add(managerUpdateStatus);

            Grid updateActions = new Grid();
            updateActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            updateActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            updateActions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            checkUpdate = CreateButton("Check for updates", false);
            AutomationProperties.SetName(checkUpdate, "Check for MyVibe updates");
            updateActions.Children.Add(checkUpdate);
            Button history = CreateButton("Version history", false);
            history.Click += ShowVersionHistoryClick;
            Grid.SetColumn(history, 2);
            updateActions.Children.Add(history);
            content.Children.Add(updateActions);

            updateAll = CreateButton("Update all plugins", true);
            updateAll.HorizontalAlignment = HorizontalAlignment.Stretch;
            updateAll.Margin = new Thickness(0, 10, 0, 0);
            updateAll.Visibility = Visibility.Collapsed;
            AutomationProperties.SetName(updateAll, "Update all available plugins");
            content.Children.Add(updateAll);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = content };
            return new Border
            {
                Background = Brush(16, 18, 21),
                BorderBrush = Structure,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Child = scroll
            };
        }

        private async void PrimaryActionClick(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            PluginDefinition plugin = _selectedPlugin;
            InstallState state = PluginInstaller.GetState(plugin);
            CatalogPackage availableUpdate;
            if (_availablePluginUpdates.TryGetValue(plugin.Id, out availableUpdate) && state == InstallState.Installed)
            {
                if (PluginInstaller.IsIllustratorRunning())
                {
                    ShowOperation("Close Illustrator, then try again.", true);
                    return;
                }
                SetBusy(true, "Downloading verified plugin update...");
                OperationResult update = await DownloadAndInstallPluginAsync(plugin, availableUpdate);
                if (update.Success)
                    _availablePluginUpdates.Remove(plugin.Id);
                SetBusy(false, update.Message);
                if (!update.Success)
                    ShowOperation(update.Message, true);
                RefreshState();
                return;
            }
            if (state == InstallState.Installed)
            {
                MessageBoxResult result = MessageBox.Show(
                    this,
                    "Remove " + plugin.Name + " from Illustrator?\n\nMyVibe will save a local backup before removing the native engine and CEP interface.",
                    "Remove plugin",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning,
                    MessageBoxResult.Cancel);
                if (result != MessageBoxResult.OK)
                    return;
            }

            if (PluginInstaller.IsIllustratorRunning())
            {
                ShowOperation("Close Illustrator, then try again.", true);
                return;
            }

            bool remove = state == InstallState.Installed;
            OperationResult operation;
            if (!remove && String.IsNullOrEmpty(plugin.PayloadResource))
            {
                CatalogPackage package = await EnsureCatalogPackageAsync(plugin);
                if (package == null)
                {
                    RefreshState();
                    return;
                }
                SetBusy(true, "Downloading verified " + plugin.Name + " package...");
                operation = await DownloadAndInstallPluginAsync(plugin, package);
            }
            else
            {
                SetBusy(true, remove ? "Removing plugin…" : "Installing plugin…");
                operation = PluginInstaller.IsAdministrator()
                    ? await Task.Run(delegate { return remove ? PluginInstaller.Remove(plugin) : PluginInstaller.Install(plugin); })
                    : await RunElevatedOperationAsync(plugin, remove);
            }
            SetBusy(false, operation.Message);
            if (!operation.Success)
                ShowOperation(operation.Message, true);
            RefreshState();
        }

        private async Task<CatalogPackage> EnsureCatalogPackageAsync(PluginDefinition plugin)
        {
            CatalogPackage package;
            if (_catalogPackages.TryGetValue(plugin.Id, out package))
                return package;
            SetBusy(true, "Loading the verified plugin catalog...");
            CatalogCheckResult result = await CatalogClient.CheckAsync();
            ApplyCatalogResult(result);
            SetBusy(false, result.Message);
            if (_catalogPackages.TryGetValue(plugin.Id, out package))
                return package;
            ShowOperation(plugin.Name + " is not available in the verified catalog yet.", true);
            return null;
        }

        private async void UpdateAllPluginsClick(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            List<KeyValuePair<PluginDefinition, CatalogPackage>> updates = PluginRegistry.All
                .Where(delegate(PluginDefinition plugin)
                {
                    return PluginInstaller.GetState(plugin) == InstallState.Installed
                        && _availablePluginUpdates.ContainsKey(plugin.Id);
                })
                .Select(delegate(PluginDefinition plugin)
                {
                    return new KeyValuePair<PluginDefinition, CatalogPackage>(plugin, _availablePluginUpdates[plugin.Id]);
                })
                .ToList();
            if (updates.Count == 0)
            {
                ShowOperation("No installed plugin updates are available.", false);
                RefreshState();
                return;
            }
            if (PluginInstaller.IsIllustratorRunning())
            {
                ShowOperation("Close Illustrator, then try again.", true);
                return;
            }

            string names = String.Join("\n", updates.Select(delegate(KeyValuePair<PluginDefinition, CatalogPackage> item)
            {
                return "• " + item.Key.Name + " " + item.Value.version;
            }).ToArray());
            MessageBoxResult confirmation = MessageBox.Show(
                this,
                "Install all available plugin updates?\n\n" + names + "\n\nMyVibe will verify every package and save a backup before replacing each plugin.",
                "Update all plugins",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information,
                MessageBoxResult.Cancel);
            if (confirmation != MessageBoxResult.OK)
                return;

            SetBusy(true, "Downloading verified plugin updates...");
            List<PluginPackageRequest> packages = new List<PluginPackageRequest>();
            foreach (KeyValuePair<PluginDefinition, CatalogPackage> item in updates)
            {
                ShowOperation("Downloading " + item.Key.Name + " " + item.Value.version + "...", false);
                OperationResult download = await CatalogClient.DownloadPluginAsync(item.Value);
                if (!download.Success)
                {
                    SetBusy(false, download.Message);
                    ShowOperation(download.Message, true);
                    RefreshState();
                    return;
                }
                packages.Add(new PluginPackageRequest
                {
                    pluginId = item.Key.Id,
                    packagePath = download.Message.Split(new[] { '\n' }, 2)[0],
                    version = item.Value.version,
                    sha256 = item.Value.sha256
                });
            }

            ShowOperation("Installing " + packages.Count + " verified plugin updates...", false);
            OperationResult updateResult = PluginInstaller.IsAdministrator()
                ? await Task.Run(delegate { return PluginInstaller.InstallPackages(packages.ToArray()); })
                : await RunElevatedPackageBatchAsync(packages.ToArray());
            if (updateResult.Success)
            {
                foreach (PluginPackageRequest package in packages)
                    _availablePluginUpdates.Remove(package.pluginId);
            }
            SetBusy(false, updateResult.Message);
            if (!updateResult.Success)
                ShowOperation(updateResult.Message, true);
            RefreshState();
        }

        private static async Task<OperationResult> DownloadAndInstallPluginAsync(PluginDefinition plugin, CatalogPackage package)
        {
            OperationResult result = await CatalogClient.DownloadPluginAsync(package);
            if (!result.Success)
                return result;
            string packagePath = result.Message.Split(new[] { '\n' }, 2)[0];
            return PluginInstaller.IsAdministrator()
                ? await Task.Run(delegate { return PluginInstaller.InstallPackage(plugin, packagePath, package.version, package.sha256); })
                : await RunElevatedPackageOperationAsync(plugin, packagePath, package.version, package.sha256);
        }

        private static async Task<OperationResult> RunElevatedOperationAsync(PluginDefinition plugin, bool remove)
        {
            Directory.CreateDirectory(PluginInstaller.IpcRoot);
            string resultPath = Path.Combine(PluginInstaller.IpcRoot, "result-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Arguments = (remove ? "--elevated-remove " : "--elevated-install ") + QuoteArgument(resultPath) + " " + QuoteArgument(plugin.Id),
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process process = Process.Start(start);
                if (process == null)
                    return OperationResult.Fail("Windows could not start the administrator operation.");
                await Task.Run(delegate { process.WaitForExit(); });
                if (!File.Exists(resultPath))
                    return OperationResult.Fail(process.ExitCode == 5 ? "Administrator access was not granted." : "The administrator operation did not return a result.");
                string[] lines = File.ReadAllLines(resultPath);
                bool success = lines.Length > 0 && String.Equals(lines[0], "OK", StringComparison.Ordinal);
                string message = lines.Length > 1 ? String.Join(Environment.NewLine, lines.Skip(1).ToArray()) : "The operation did not return a message.";
                return success ? OperationResult.Ok(message) : OperationResult.Fail(message);
            }
            catch (Win32Exception ex)
            {
                return OperationResult.Fail(ex.NativeErrorCode == 1223 ? "Administrator permission was cancelled." : "Windows could not start the administrator operation. " + ex.Message);
            }
            finally
            {
                if (File.Exists(resultPath))
                    File.Delete(resultPath);
            }
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static async Task<OperationResult> RunElevatedPackageOperationAsync(PluginDefinition plugin, string packagePath, string version, string sha256)
        {
            Directory.CreateDirectory(PluginInstaller.IpcRoot);
            string resultPath = Path.Combine(PluginInstaller.IpcRoot, "result-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Arguments = "--elevated-install-package " + QuoteArgument(resultPath) + " " + QuoteArgument(plugin.Id) + " " + QuoteArgument(packagePath) + " " + QuoteArgument(version) + " " + QuoteArgument(sha256),
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process process = Process.Start(start);
                if (process == null)
                    return OperationResult.Fail("Windows could not start the administrator operation.");
                await Task.Run(delegate { process.WaitForExit(); });
                if (!File.Exists(resultPath))
                    return OperationResult.Fail(process.ExitCode == 5 ? "Administrator access was not granted." : "The administrator operation did not return a result.");
                string[] lines = File.ReadAllLines(resultPath);
                bool success = lines.Length > 0 && String.Equals(lines[0], "OK", StringComparison.Ordinal);
                string message = lines.Length > 1 ? String.Join(Environment.NewLine, lines.Skip(1).ToArray()) : "The operation did not return a message.";
                return success ? OperationResult.Ok(message) : OperationResult.Fail(message);
            }
            catch (Win32Exception ex)
            {
                return OperationResult.Fail(ex.NativeErrorCode == 1223 ? "Administrator permission was cancelled." : "Windows could not start the administrator operation. " + ex.Message);
            }
            finally
            {
                if (File.Exists(resultPath))
                    File.Delete(resultPath);
            }
        }

        private static async Task<OperationResult> RunElevatedPackageBatchAsync(PluginPackageRequest[] packages)
        {
            Directory.CreateDirectory(PluginInstaller.IpcRoot);
            string token = Guid.NewGuid().ToString("N");
            string resultPath = Path.Combine(PluginInstaller.IpcRoot, "result-" + token + ".txt");
            string requestPath = Path.Combine(PluginInstaller.IpcRoot, "updates-" + token + ".json");
            try
            {
                File.WriteAllText(requestPath, new JavaScriptSerializer().Serialize(packages), new System.Text.UTF8Encoding(false));
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    Arguments = "--elevated-update-packages " + QuoteArgument(resultPath) + " " + QuoteArgument(requestPath),
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process process = Process.Start(start);
                if (process == null)
                    return OperationResult.Fail("Windows could not start the administrator operation.");
                await Task.Run(delegate { process.WaitForExit(); });
                if (!File.Exists(resultPath))
                    return OperationResult.Fail(process.ExitCode == 5 ? "Administrator access was not granted." : "The administrator operation did not return a result.");
                string[] lines = File.ReadAllLines(resultPath);
                bool success = lines.Length > 0 && String.Equals(lines[0], "OK", StringComparison.Ordinal);
                string message = lines.Length > 1 ? String.Join(Environment.NewLine, lines.Skip(1).ToArray()) : "The operation did not return a message.";
                return success ? OperationResult.Ok(message) : OperationResult.Fail(message);
            }
            catch (Win32Exception ex)
            {
                return OperationResult.Fail(ex.NativeErrorCode == 1223 ? "Administrator permission was cancelled." : "Windows could not start the administrator operation. " + ex.Message);
            }
            finally
            {
                if (File.Exists(resultPath))
                    File.Delete(resultPath);
                if (File.Exists(requestPath))
                    File.Delete(requestPath);
            }
        }

        private async void CheckForUpdatesClick(object sender, RoutedEventArgs e)
        {
            if (_busy)
                return;

            if (_availableManagerUpdate != null)
            {
                MessageBoxResult confirmation = MessageBox.Show(
                    this,
                    "Install MyVibe " + _availableManagerUpdate.version + " now?\n\nMyVibe will verify the release, store the current version for rollback, replace itself, and restart.",
                    "Update MyVibe",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Information,
                    MessageBoxResult.Cancel);
                if (confirmation != MessageBoxResult.OK)
                    return;
                _checkUpdateButton.IsEnabled = false;
                _managerUpdateStatus.Text = "Downloading and verifying MyVibe...";
                OperationResult managerResult = await ManagerUpdates.DownloadAndStartAsync(_availableManagerUpdate);
                _managerUpdateStatus.Text = managerResult.Message;
                _checkUpdateButton.IsEnabled = true;
                if (managerResult.Success)
                {
                    Application.Current.Shutdown();
                    return;
                }
                return;
            }

            _checkUpdateButton.IsEnabled = false;
            _managerUpdateStatus.Text = "Checking GitHub releases…";
            CatalogCheckResult result = await CatalogClient.CheckAsync();
            _managerUpdateStatus.Text = result.Message;
            ApplyCatalogResult(result);
            _checkUpdateButton.Content = _availableManagerUpdate == null ? "Check for updates" : "Update MyVibe to " + _availableManagerUpdate.version;
            _checkUpdateButton.IsEnabled = true;
            RefreshState();
        }

        private void ShowVersionHistoryClick(object sender, RoutedEventArgs e)
        {
            string[] versions = ManagerUpdates.GetStoredVersions();
            if (versions.Length == 0)
            {
                MessageBox.Show(this, "No previous MyVibe versions are stored yet. A restorable version will appear here after the first manager update.", "MyVibe version history", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string version = versions[0];
            MessageBoxResult confirmation = MessageBox.Show(
                this,
                "Stored MyVibe versions:\n\n" + String.Join("\n", versions) + "\n\nRestore the newest stored version (" + version + ")?",
                "MyVibe version history",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes)
                return;
            OperationResult result = ManagerUpdates.StartStoredVersion(version);
            if (result.Success)
                Application.Current.Shutdown();
            else
                MessageBox.Show(this, result.Message, "MyVibe rollback", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void RefreshState()
        {
            int installedCount = 0;
            int incompleteCount = 0;
            foreach (PluginDefinition plugin in PluginRegistry.All)
            {
                InstallState pluginState = PluginInstaller.GetState(plugin);
                PluginCardBindings bindings = _cards[plugin.Id];
                if (pluginState == InstallState.Installed)
                {
                    installedCount++;
                    CatalogPackage cardUpdate;
                    if (_availablePluginUpdates.TryGetValue(plugin.Id, out cardUpdate))
                        SetPill(bindings.Pill, bindings.State, "Update " + cardUpdate.version, Accent);
                    else
                        SetPill(bindings.Pill, bindings.State, "Installed", Success);
                }
                else if (pluginState == InstallState.Incomplete)
                {
                    incompleteCount++;
                    SetPill(bindings.Pill, bindings.State, "Repair needed", Warning);
                }
                else
                    SetPill(bindings.Pill, bindings.State, "Available", Accent);
            }
            _libraryCount.Text = PluginRegistry.All.Length + " plugins · " + installedCount + " installed" + (incompleteCount > 0 ? " · " + incompleteCount + " needs attention" : String.Empty);
            int updateCount = PluginRegistry.All.Count(delegate(PluginDefinition plugin)
            {
                return PluginInstaller.GetState(plugin) == InstallState.Installed && _availablePluginUpdates.ContainsKey(plugin.Id);
            });
            _updateAllButton.Visibility = updateCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            _updateAllButton.Content = "Update all plugins (" + updateCount + ")";
            AutomationProperties.SetName(_updateAllButton, "Update all " + updateCount + " available plugins");

            InstallState state = PluginInstaller.GetState(_selectedPlugin);
            if (state == InstallState.Installed)
            {
                SetPill(_detailStatePill, _detailState, "Installed", Success);
                CatalogPackage update;
                if (_availablePluginUpdates.TryGetValue(_selectedPlugin.Id, out update))
                {
                    _primaryButton.Content = "Update to " + update.version;
                    SetPrimaryButtonAppearance(true);
                    AutomationProperties.SetName(_primaryButton, "Update " + _selectedPlugin.Name + " to " + update.version);
                }
                else
                {
                    _primaryButton.Content = "Remove plugin";
                    SetPrimaryButtonAppearance(false);
                    AutomationProperties.SetName(_primaryButton, "Remove " + _selectedPlugin.Name);
                }
            }
            else if (state == InstallState.Incomplete)
            {
                SetPill(_detailStatePill, _detailState, "Installation incomplete", Warning);
                _primaryButton.Content = "Repair installation";
                SetPrimaryButtonAppearance(true);
                AutomationProperties.SetName(_primaryButton, "Repair " + _selectedPlugin.Name + " installation");
            }
            else
            {
                SetPill(_detailStatePill, _detailState, "Available to install", Accent);
                _primaryButton.Content = "Install plugin";
                SetPrimaryButtonAppearance(true);
                AutomationProperties.SetName(_primaryButton, "Install " + _selectedPlugin.Name);
            }
        }

        private void ApplyCatalogResult(CatalogCheckResult result)
        {
            _catalogPackages.Clear();
            foreach (KeyValuePair<string, CatalogPackage> item in result.Packages)
                _catalogPackages[item.Key] = item.Value;
            _availablePluginUpdates.Clear();
            foreach (KeyValuePair<string, CatalogPackage> item in result.PluginUpdates)
                _availablePluginUpdates[item.Key] = item.Value;
            _availableManagerUpdate = result.ManagerUpdate;
        }

        private void SelectPlugin(PluginDefinition plugin, bool bringIntoView)
        {
            _selectedPlugin = plugin;
            foreach (PluginDefinition item in PluginRegistry.All)
                _cards[item.Id].Card.BorderBrush = item == plugin ? Accent : Structure;
            if (_detailIcon != null)
            {
                _detailIcon.Child = BuildPluginMark(plugin, 28);
                _detailName.Text = plugin.Name;
                _detailVersion.Text = "Version " + plugin.Version;
                _detailSummary.Text = plugin.Summary;
                _aipPathValue.Text = PluginInstaller.GetAipPath(plugin);
                _cepPathValue.Text = PluginInstaller.GetCepPath(plugin);
                ShowOperation(String.Empty, false);
                RefreshState();
            }
            if (bringIntoView && _detailPane != null)
                _detailPane.BringIntoView();
        }

        private void SetBusy(bool busy, string message)
        {
            _busy = busy;
            _primaryButton.IsEnabled = !busy;
            _checkUpdateButton.IsEnabled = !busy;
            _updateAllButton.IsEnabled = !busy;
            _progress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            ShowOperation(message, false);
        }

        private void ShowOperation(string message, bool error)
        {
            _operationMessage.Text = message;
            _operationMessage.Foreground = error ? Danger : Muted;
        }

        private void UpdateResponsiveLayout()
        {
            bool narrow = ActualWidth < 840;
            if (narrow)
            {
                _workspace.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                _workspace.ColumnDefinitions[1].Width = new GridLength(0);
                _workspace.RowDefinitions[0].Height = new GridLength(230);
                _workspace.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                Grid.SetColumn(_libraryPane, 0);
                Grid.SetRow(_libraryPane, 0);
                Grid.SetColumn(_detailPane, 0);
                Grid.SetRow(_detailPane, 1);
                _detailPane.BorderThickness = new Thickness(0, 1, 0, 0);
            }
            else
            {
                _workspace.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                _workspace.ColumnDefinitions[1].Width = new GridLength(410);
                _workspace.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                _workspace.RowDefinitions[1].Height = GridLength.Auto;
                Grid.SetColumn(_libraryPane, 0);
                Grid.SetRow(_libraryPane, 0);
                Grid.SetColumn(_detailPane, 1);
                Grid.SetRow(_detailPane, 0);
                _detailPane.BorderThickness = new Thickness(1, 0, 0, 0);
            }
        }

        private void SetPrimaryButtonAppearance(bool primary)
        {
            SolidColorBrush normal = primary ? AccentPressed : Raised;
            SolidColorBrush hover = primary ? AccentHover : Hover;
            SolidColorBrush pressed = primary ? AccentPressed : Surface;
            _primaryButton.Background = normal;
            _primaryButton.BorderBrush = primary ? Accent : Structure;
            _primaryButton.FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal;
            _primaryButton.Template = BuildButtonTemplate(normal, hover, pressed, new CornerRadius(8));
        }

        private static Button CreateButton(string text, bool primary)
        {
            SolidColorBrush normal = primary ? AccentPressed : Raised;
            SolidColorBrush hover = primary ? AccentHover : Hover;
            SolidColorBrush pressed = primary ? AccentPressed : Surface;
            Button button = new Button
            {
                Content = text,
                Foreground = Text,
                Background = normal,
                BorderBrush = primary ? Accent : Structure,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 7, 12, 7),
                MinHeight = 38,
                FontWeight = primary ? FontWeights.SemiBold : FontWeights.Normal,
                Cursor = Cursors.Hand,
                Template = BuildButtonTemplate(normal, hover, pressed, new CornerRadius(8)),
                FocusVisualStyle = BuildFocusStyle(new CornerRadius(9))
            };
            return button;
        }

        private static ControlTemplate BuildButtonTemplate(Brush normal, Brush hover, Brush pressed, CornerRadius radius)
        {
            FrameworkElementFactory chrome = new FrameworkElementFactory(typeof(Border));
            chrome.Name = "Chrome";
            chrome.SetValue(Border.BackgroundProperty, normal);
            chrome.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            chrome.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            chrome.SetValue(Border.CornerRadiusProperty, radius);
            chrome.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));

            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            chrome.AppendChild(presenter);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = chrome;
            Trigger hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, hover, "Chrome"));
            template.Triggers.Add(hoverTrigger);
            Trigger pressedTrigger = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, pressed, "Chrome"));
            template.Triggers.Add(pressedTrigger);
            Trigger disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.42));
            template.Triggers.Add(disabled);
            return template;
        }

        private static Style BuildFocusStyle(CornerRadius radius)
        {
            FrameworkElementFactory visual = new FrameworkElementFactory(typeof(Border));
            visual.SetValue(Border.BorderBrushProperty, Accent);
            visual.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            visual.SetValue(Border.CornerRadiusProperty, radius);
            ControlTemplate template = new ControlTemplate();
            template.VisualTree = visual;
            Style style = new Style();
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static Border MetaChip(string text)
        {
            return new Border
            {
                Background = Raised,
                BorderBrush = Structure,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 7, 7),
                Child = new TextBlock { Text = text, Foreground = Muted, FontSize = 11 }
            };
        }

        private static TextBlock SectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Subtle,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 25, 0, 10)
            };
        }

        private static Grid MetaRow(string label, string value)
        {
            Grid row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = label, Foreground = Muted });
            TextBlock valueText = new TextBlock { Text = value, Foreground = Text, TextAlignment = TextAlignment.Right };
            Grid.SetColumn(valueText, 1);
            row.Children.Add(valueText);
            return row;
        }

        private static Grid ComponentRow(string label, string value, SolidColorBrush color = null)
        {
            Grid row = MetaRow(label, value);
            ((TextBlock)row.Children[1]).Foreground = color ?? Success;
            return row;
        }

        private static Border PathText(TextBlock value)
        {
            return new Border
            {
                Background = Surface,
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(9, 7, 9, 7),
                Margin = new Thickness(0, 0, 0, 7),
                Child = value
            };
        }

        private static Viewbox BuildPluginMark(PluginDefinition plugin, double size)
        {
            return plugin.IsToneMesh ? BuildToneMeshMark(size) : BuildCubeMark(size);
        }

        private static Viewbox BuildCubeMark(double size)
        {
            Canvas canvas = new Canvas { Width = 48, Height = 48 };
            Polyline outer = new Polyline
            {
                Stroke = Accent,
                StrokeThickness = 2.2,
                Points = new PointCollection
                {
                    new Point(24, 5), new Point(43, 16), new Point(43, 32), new Point(24, 43),
                    new Point(5, 32), new Point(5, 16), new Point(24, 5)
                }
            };
            canvas.Children.Add(outer);
            canvas.Children.Add(Line(24, 23, 24, 43));
            canvas.Children.Add(Line(24, 23, 5, 16));
            canvas.Children.Add(Line(24, 23, 43, 16));
            return new Viewbox { Width = size, Height = size, Child = canvas, Stretch = Stretch.Uniform };
        }

        private static Viewbox BuildMyVibeMark(double size)
        {
            Canvas canvas = new Canvas { Width = 48, Height = 48 };
            Polyline mark = new Polyline
            {
                Stroke = Accent,
                StrokeThickness = 4.6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Points = new PointCollection
                {
                    new Point(8, 36), new Point(8, 12), new Point(24, 30),
                    new Point(40, 12), new Point(40, 36)
                }
            };
            canvas.Children.Add(mark);
            return new Viewbox { Width = size, Height = size, Child = canvas, Stretch = Stretch.Uniform };
        }

        private static Viewbox BuildToneMeshMark(double size)
        {
            Canvas canvas = new Canvas { Width = 48, Height = 48 };
            double[] radii = { 2.2, 3.2, 4.3, 3.2, 4.3, 5.5, 4.3, 5.5, 6.8 };
            int index = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    double radius = radii[index++];
                    System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse
                    {
                        Width = radius * 2,
                        Height = radius * 2,
                        Fill = Accent
                    };
                    Canvas.SetLeft(dot, 9 + column * 14 - radius);
                    Canvas.SetTop(dot, 9 + row * 14 - radius);
                    canvas.Children.Add(dot);
                }
            }
            return new Viewbox { Width = size, Height = size, Child = canvas, Stretch = Stretch.Uniform };
        }

        private static Line Line(double x1, double y1, double x2, double y2)
        {
            return new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = Structure, StrokeThickness = 1.5 };
        }

        private static void SetPill(Border pill, TextBlock label, string text, SolidColorBrush color)
        {
            label.Text = text;
            label.Foreground = color;
            pill.Background = WithAlpha(color, 28);
            pill.BorderBrush = WithAlpha(color, 90);
        }

        private static SolidColorBrush Brush(byte r, byte g, byte b)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush WithAlpha(SolidColorBrush source, byte alpha)
        {
            Color color = source.Color;
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
            brush.Freeze();
            return brush;
        }
    }

    internal sealed class OperationResult
    {
        internal bool Success;
        internal string Message;

        internal static OperationResult Ok(string message)
        {
            return new OperationResult { Success = true, Message = message };
        }

        internal static OperationResult Fail(string message)
        {
            return new OperationResult { Success = false, Message = message };
        }
    }

    internal sealed class PluginPackageRequest
    {
        public string pluginId;
        public string packagePath;
        public string version;
        public string sha256;
    }

    internal sealed class BackupSnapshot
    {
        internal string Root;
        internal bool HadAip;
        internal bool HadCep;
    }

    internal static class PluginInstaller
    {
        private const long MaxPluginExtractedBytes = 500L * 1024 * 1024;
        private const int MaxPluginArchiveEntries = 10000;
        internal static string IllustratorRoot
        {
            get
            {
                string adobeRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Adobe");
                if (Directory.Exists(adobeRoot))
                {
                    string detected = Directory.GetDirectories(adobeRoot, "Adobe Illustrator 2026*", SearchOption.TopDirectoryOnly)
                        .OrderBy(delegate(string path) { return path.Length; })
                        .FirstOrDefault();
                    if (!String.IsNullOrEmpty(detected))
                        return detected;
                }
                return Path.Combine(adobeRoot, "Adobe Illustrator 2026");
            }
        }
        internal static readonly string IpcRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "ipc");

        internal static string GetAipPath(PluginDefinition plugin)
        {
            return Path.Combine(IllustratorRoot, "Plug-ins", plugin.AipFileName);
        }

        internal static string GetCepPath(PluginDefinition plugin)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adobe", "CEP", "extensions", plugin.CepFolderName);
        }

        internal static InstallState GetState(PluginDefinition plugin)
        {
            string aipPath = GetAipPath(plugin);
            string cepPath = GetCepPath(plugin);
            bool aip = File.Exists(aipPath);
            bool cep = Directory.Exists(cepPath) && File.Exists(Path.Combine(cepPath, @"CSXS\manifest.xml"));
            if (aip && cep)
                return InstallState.Installed;
            if (aip || cep)
                return InstallState.Incomplete;
            return InstallState.Available;
        }

        internal static bool IsIllustratorRunning()
        {
            try
            {
                return Process.GetProcessesByName("Illustrator").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static bool IsSafeResultPath(string path)
        {
            try
            {
                string root = Path.GetFullPath(IpcRoot) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(path);
                return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                    && Path.GetFileName(candidate).StartsWith("result-", StringComparison.OrdinalIgnoreCase)
                    && String.Equals(Path.GetExtension(candidate), ".txt", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsSafeBatchRequestPath(string path)
        {
            try
            {
                string root = Path.GetFullPath(IpcRoot) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(path);
                string fileName = Path.GetFileName(candidate);
                return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                    && fileName.StartsWith("updates-", StringComparison.OrdinalIgnoreCase)
                    && String.Equals(Path.GetExtension(candidate), ".json", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(candidate);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsSupportedIllustratorInstalled()
        {
            return Directory.Exists(IllustratorRoot);
        }

        internal static bool HasNativeRuntime()
        {
            string system = Environment.SystemDirectory;
            return File.Exists(Path.Combine(system, "MSVCP140.dll"))
                && File.Exists(Path.Combine(system, "VCRUNTIME140.dll"))
                && File.Exists(Path.Combine(system, "VCRUNTIME140_1.dll"));
        }

        internal static OperationResult Install(PluginDefinition plugin)
        {
            if (String.IsNullOrEmpty(plugin.PayloadResource))
                return OperationResult.Fail(plugin.Name + " requires the verified online package.");
            if (!IsSupportedIllustratorInstalled())
                return OperationResult.Fail("Adobe Illustrator 2026 was not found. Install Illustrator 2026, then try again.");
            if (!HasNativeRuntime())
                return OperationResult.Fail("Microsoft Visual C++ 2015–2022 Redistributable (x64) is required before installing this plugin.");
            if (IsIllustratorRunning())
                return OperationResult.Fail("Close Illustrator, then try again.");

            BackupSnapshot backup = null;
            try
            {
                backup = BackupCurrent(plugin, "before-install");
                DeleteCurrent(plugin);
                ExtractPayload(plugin);
                WriteVersionRecord(plugin, plugin.Version);
                ClearCepCache(plugin);
                return OperationResult.Ok("Installed " + plugin.Name + " " + plugin.Version + ". Restart Illustrator to load the plugin.");
            }
            catch (Exception ex)
            {
                try
                {
                    DeleteCurrent(plugin);
                    Restore(plugin, backup);
                }
                catch
                {
                    return OperationResult.Fail("Installation failed and the previous copy could not be fully restored. " + FriendlyError(ex));
                }
                return OperationResult.Fail("Installation failed. The previous copy was restored. " + FriendlyError(ex));
            }
        }

        internal static OperationResult InstallPackage(PluginDefinition plugin, string packagePath, string version, string expectedSha256)
        {
            System.Version parsedVersion;
            if (!CatalogClient.IsSafeDownloadPath(packagePath) || !System.Version.TryParse(version, out parsedVersion))
                return OperationResult.Fail("The downloaded plugin package metadata is invalid.");
            if (!CatalogClient.IsAuthorizedPluginPackage(plugin.Id, version, expectedSha256, packagePath))
                return OperationResult.Fail("The downloaded plugin package is not authorized by the verified MyVibe catalog.");
            if (!IsSupportedIllustratorInstalled())
                return OperationResult.Fail("Adobe Illustrator 2026 was not found. Install Illustrator 2026, then try again.");
            if (!HasNativeRuntime())
                return OperationResult.Fail("Microsoft Visual C++ 2015-2022 Redistributable (x64) is required before installing this plugin.");
            if (IsIllustratorRunning())
                return OperationResult.Fail("Close Illustrator, then try again.");

            BackupSnapshot backup = null;
            try
            {
                backup = BackupCurrent(plugin, "before-update");
                DeleteCurrent(plugin);
                using (FileStream stream = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    ExtractPayload(plugin, stream);
                WriteVersionRecord(plugin, version);
                ClearCepCache(plugin);
                return OperationResult.Ok("Installed " + plugin.Name + " " + version + ". Restart Illustrator to load the plugin.");
            }
            catch (Exception ex)
            {
                try
                {
                    DeleteCurrent(plugin);
                    Restore(plugin, backup);
                }
                catch
                {
                    return OperationResult.Fail("Update failed and the previous copy could not be fully restored. " + FriendlyError(ex));
                }
                return OperationResult.Fail("Update failed. The previous copy was restored. " + FriendlyError(ex));
            }
        }

        internal static OperationResult InstallPackagesFromRequest(string requestPath)
        {
            try
            {
                if (!IsAdministrator() || !IsSafeBatchRequestPath(requestPath))
                    return OperationResult.Fail("The plugin update request is not authorized.");
                FileInfo request = new FileInfo(requestPath);
                if (request.Length == 0 || request.Length > 64 * 1024)
                    return OperationResult.Fail("The plugin update request is invalid.");
                PluginPackageRequest[] packages = new JavaScriptSerializer().Deserialize<PluginPackageRequest[]>(File.ReadAllText(requestPath));
                return InstallPackages(packages);
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("The plugin update request could not be processed. " + FriendlyError(ex));
            }
        }

        internal static OperationResult InstallPackages(PluginPackageRequest[] packages)
        {
            if (packages == null || packages.Length == 0 || packages.Length > PluginRegistry.All.Length)
                return OperationResult.Fail("The plugin update request is empty or too large.");
            if (!IsSupportedIllustratorInstalled())
                return OperationResult.Fail("Adobe Illustrator 2026 was not found. Install Illustrator 2026, then try again.");
            if (!HasNativeRuntime())
                return OperationResult.Fail("Microsoft Visual C++ 2015-2022 Redistributable (x64) is required before installing these plugins.");
            if (IsIllustratorRunning())
                return OperationResult.Fail("Close Illustrator, then try again.");

            HashSet<string> pluginIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PluginPackageRequest package in packages)
            {
                PluginDefinition plugin = package == null ? null : PluginRegistry.Find(package.pluginId);
                System.Version parsedVersion;
                if (plugin == null || !pluginIds.Add(plugin.Id)
                    || !CatalogClient.IsSafeDownloadPath(package.packagePath)
                    || !System.Version.TryParse(package.version, out parsedVersion)
                    || !CatalogClient.IsAuthorizedPluginPackage(plugin.Id, package.version, package.sha256, package.packagePath))
                    return OperationResult.Fail("One or more plugin update packages are not authorized by the verified MyVibe catalog.");
            }

            int completed = 0;
            foreach (PluginPackageRequest package in packages)
            {
                PluginDefinition plugin = PluginRegistry.Find(package.pluginId);
                OperationResult result = InstallPackage(plugin, package.packagePath, package.version, package.sha256);
                if (!result.Success)
                    return OperationResult.Fail((completed == 0 ? String.Empty : completed + " plugin update(s) completed before the failure. ") + result.Message);
                completed++;
            }
            return OperationResult.Ok("Updated " + completed + " plugin" + (completed == 1 ? String.Empty : "s") + ". Restart Illustrator to load the updates.");
        }

        internal static string GetInstalledVersion(PluginDefinition plugin)
        {
            string record = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "state", plugin.StateFileName);
            try
            {
                string value = File.Exists(record) ? File.ReadAllText(record).Trim() : String.Empty;
                System.Version parsed;
                if (System.Version.TryParse(value, out parsed))
                    return value;
            }
            catch
            {
            }
            if (GetState(plugin) != InstallState.Installed)
                return null;
            string manifestVersion = ReadCepManifestVersion(Path.Combine(GetCepPath(plugin), @"CSXS\manifest.xml"));
            return manifestVersion ?? "0.0.0";
        }

        internal static string ParseCepManifestVersionForTest(string xml)
        {
            try
            {
                using (StringReader input = new StringReader(xml))
                using (XmlReader reader = XmlReader.Create(input, CreateManifestReaderSettings()))
                    return ParseCepManifestVersion(reader);
            }
            catch
            {
                return null;
            }
        }

        private static string ReadCepManifestVersion(string manifestPath)
        {
            try
            {
                FileInfo manifest = new FileInfo(manifestPath);
                if (!manifest.Exists || manifest.Length == 0 || manifest.Length > 1024 * 1024)
                    return null;
                using (FileStream input = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (XmlReader reader = XmlReader.Create(input, CreateManifestReaderSettings()))
                    return ParseCepManifestVersion(reader);
            }
            catch
            {
                return null;
            }
        }

        private static XmlReaderSettings CreateManifestReaderSettings()
        {
            return new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = 1024 * 1024
            };
        }

        private static string ParseCepManifestVersion(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element || !String.Equals(reader.LocalName, "ExtensionManifest", StringComparison.Ordinal))
                    continue;
                string value = reader.GetAttribute("ExtensionBundleVersion");
                System.Version parsed;
                return System.Version.TryParse(value, out parsed) ? value : null;
            }
            return null;
        }

        internal static OperationResult Remove(PluginDefinition plugin)
        {
            if (IsIllustratorRunning())
                return OperationResult.Fail("Close Illustrator, then try again.");

            BackupSnapshot backup = null;
            try
            {
                backup = BackupCurrent(plugin, "before-remove");
                DeleteCurrent(plugin);
                ClearCepCache(plugin);
                string suffix = backup.HadAip || backup.HadCep ? " A backup was saved in " + backup.Root + "." : String.Empty;
                return OperationResult.Ok("Removed " + plugin.Name + ". Restart Illustrator to refresh the Extensions menu." + suffix);
            }
            catch (Exception ex)
            {
                try
                {
                    DeleteCurrent(plugin);
                    Restore(plugin, backup);
                }
                catch
                {
                    return OperationResult.Fail("Removal failed and the previous copy could not be fully restored. " + FriendlyError(ex));
                }
                return OperationResult.Fail("Removal failed. The previous copy was restored. " + FriendlyError(ex));
            }
        }

        internal static bool ValidatePayload(PluginDefinition plugin, out string error)
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(plugin.PayloadResource))
                {
                    if (stream == null)
                    {
                        error = "embedded payload is missing";
                        return false;
                    }
                    using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, false))
                    {
                        string aipSuffix = "/" + plugin.AipFileName;
                        string cepMarker = "/" + plugin.CepFolderName + "/";
                        bool aip = archive.Entries.Any(delegate(ZipArchiveEntry entry) { return Normalize(entry.FullName).EndsWith(aipSuffix, StringComparison.OrdinalIgnoreCase); });
                        bool manifest = archive.Entries.Any(delegate(ZipArchiveEntry entry) { return Normalize(entry.FullName).EndsWith(cepMarker + "CSXS/manifest.xml", StringComparison.OrdinalIgnoreCase); });
                        bool signature = archive.Entries.Any(delegate(ZipArchiveEntry entry) { return Normalize(entry.FullName).EndsWith(cepMarker + "META-INF/signatures.xml", StringComparison.OrdinalIgnoreCase); });
                        if (!aip || !manifest || !signature)
                        {
                            error = "payload does not contain the native engine, CEP manifest, and CEP signature";
                            return false;
                        }
                    }
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void ExtractPayload(PluginDefinition plugin)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(plugin.PayloadResource))
            {
                if (stream == null)
                    throw new InvalidOperationException("The embedded plugin payload is missing.");
                ExtractPayload(plugin, stream);
            }
        }

        private static void ExtractPayload(PluginDefinition plugin, Stream stream)
        {
            string aipPath = GetAipPath(plugin);
            string cepPath = GetCepPath(plugin);
            string aipSuffix = "/" + plugin.AipFileName;
            string cepMarker = "/" + plugin.CepFolderName + "/";
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                    if (archive.Entries.Count > MaxPluginArchiveEntries)
                        throw new InvalidDataException("The plugin payload contains too many files.");
                    ZipArchiveEntry aip = archive.Entries.FirstOrDefault(delegate(ZipArchiveEntry entry)
                    {
                        return Normalize(entry.FullName).EndsWith(aipSuffix, StringComparison.OrdinalIgnoreCase);
                    });
                    if (aip == null)
                        throw new InvalidDataException("The native .aip engine is missing from the payload.");
                    bool manifest = archive.Entries.Any(delegate(ZipArchiveEntry entry) { return Normalize(entry.FullName).EndsWith(cepMarker + "CSXS/manifest.xml", StringComparison.OrdinalIgnoreCase); });
                    bool signature = archive.Entries.Any(delegate(ZipArchiveEntry entry) { return Normalize(entry.FullName).EndsWith(cepMarker + "META-INF/signatures.xml", StringComparison.OrdinalIgnoreCase); });
                    if (!manifest || !signature)
                        throw new InvalidDataException("The plugin payload does not contain a signed CEP interface.");
                    long extractedBytes = aip.Length;
                    if (extractedBytes > MaxPluginExtractedBytes)
                        throw new InvalidDataException("The plugin payload is too large after extraction.");

                    Directory.CreateDirectory(Path.GetDirectoryName(aipPath));
                    CopyEntry(aip, aipPath);

                    string cepRoot = Path.GetFullPath(cepPath) + Path.DirectorySeparatorChar;
                    Directory.CreateDirectory(cepPath);
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string normalized = Normalize(entry.FullName);
                        int marker = normalized.IndexOf(cepMarker, StringComparison.OrdinalIgnoreCase);
                        if (marker < 0)
                            continue;

                        if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                            throw new InvalidDataException("The plugin payload contains an unsafe link.");
                        extractedBytes += entry.Length;
                        if (extractedBytes > MaxPluginExtractedBytes)
                            throw new InvalidDataException("The plugin payload is too large after extraction.");

                        string relative = normalized.Substring(marker + cepMarker.Length).Replace('/', Path.DirectorySeparatorChar);
                        if (String.IsNullOrEmpty(relative))
                            continue;
                        string target = Path.GetFullPath(Path.Combine(cepPath, relative));
                        if (!target.StartsWith(cepRoot, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("The plugin payload contains an unsafe path.");
                        if (String.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(target);
                            continue;
                        }
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        CopyEntry(entry, target);
                    }
            }
        }

        private static void CopyEntry(ZipArchiveEntry entry, string target)
        {
            using (Stream input = entry.Open())
            using (FileStream output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None))
                input.CopyTo(output);
        }

        private static BackupSnapshot BackupCurrent(PluginDefinition plugin, string reason)
        {
            string aipPath = GetAipPath(plugin);
            string cepPath = GetCepPath(plugin);
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyVibe",
                "backups",
                plugin.Name,
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + reason);
            BackupSnapshot snapshot = new BackupSnapshot { Root = root, HadAip = File.Exists(aipPath), HadCep = Directory.Exists(cepPath) };
            if (!snapshot.HadAip && !snapshot.HadCep)
                return snapshot;
            Directory.CreateDirectory(root);
            if (snapshot.HadAip)
                File.Copy(aipPath, Path.Combine(root, plugin.AipFileName), true);
            if (snapshot.HadCep)
                CopyDirectory(cepPath, Path.Combine(root, "CEP"));
            return snapshot;
        }

        private static void Restore(PluginDefinition plugin, BackupSnapshot snapshot)
        {
            if (snapshot == null)
                return;
            if (snapshot.HadAip)
            {
                string aipPath = GetAipPath(plugin);
                Directory.CreateDirectory(Path.GetDirectoryName(aipPath));
                File.Copy(Path.Combine(snapshot.Root, plugin.AipFileName), aipPath, true);
            }
            if (snapshot.HadCep)
                CopyDirectory(Path.Combine(snapshot.Root, "CEP"), GetCepPath(plugin));
        }

        private static void DeleteCurrent(PluginDefinition plugin)
        {
            string aipPath = GetAipPath(plugin);
            string cepPath = GetCepPath(plugin);
            if (File.Exists(aipPath))
                File.Delete(aipPath);
            if (Directory.Exists(cepPath))
                Directory.Delete(cepPath, true);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(directory.Replace(source, destination));
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = file.Replace(source, destination);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(file, target, true);
            }
        }

        private static void ClearCepCache(PluginDefinition plugin)
        {
            string cache = Path.Combine(Path.GetTempPath(), "cep_cache");
            if (!Directory.Exists(cache))
                return;
            foreach (string directory in Directory.GetDirectories(cache, plugin.CachePattern, SearchOption.TopDirectoryOnly))
                Directory.Delete(directory, true);
        }

        private static void WriteVersionRecord(PluginDefinition plugin, string version)
        {
            string state = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "state");
            Directory.CreateDirectory(state);
            File.WriteAllText(Path.Combine(state, plugin.StateFileName), version);
        }

        private static string Normalize(string path)
        {
            return "/" + path.Replace('\\', '/').TrimStart('/');
        }

        private static string FriendlyError(Exception ex)
        {
            if (ex is UnauthorizedAccessException)
                return "Administrator access was denied.";
            if (ex is IOException)
                return "A plugin file is in use. Close Illustrator and try again.";
            return ex.Message;
        }
    }

    internal sealed class PublicCatalog
    {
        public int schemaVersion;
        public string channel;
        public CatalogPackage manager;
        public CatalogPackage[] plugins;
    }

    internal sealed class PublicCatalogV2
    {
        public int schemaVersion;
        public string channel;
        public CatalogProductV2 manager;
        public CatalogProductV2[] plugins;
    }

    internal sealed class CatalogProductV2
    {
        public string id;
        public string name;
        public string version;
        public string description;
        public string host;
        public string hostVersion;
        public string minimumManagerVersion;
        public CatalogArtifactV2[] artifacts;
    }

    internal sealed class CatalogArtifactV2
    {
        public string platform;
        public string architecture;
        public string downloadUrl;
        public string sha256;
        public CatalogSignatureV2 signature;
    }

    internal sealed class CatalogSignatureV2
    {
        public string type;
        public bool required;
        public string signerThumbprint;
    }

    internal sealed class CatalogPackage
    {
        public string id;
        public string name;
        public string version;
        public string platform;
        public string architecture;
        public string downloadUrl;
        public string sha256;
        public bool authenticodeRequired;
        public string host;
        public string hostVersion;
        public string minimumManagerVersion;
        public string signerThumbprint;
    }

    internal sealed class CatalogCheckResult
    {
        internal string Message;
        internal readonly Dictionary<string, CatalogPackage> Packages = new Dictionary<string, CatalogPackage>(StringComparer.OrdinalIgnoreCase);
        internal readonly Dictionary<string, CatalogPackage> PluginUpdates = new Dictionary<string, CatalogPackage>(StringComparer.OrdinalIgnoreCase);
        internal CatalogPackage ManagerUpdate;
    }

    internal static class CatalogClient
    {
        private const string CatalogV2Url = "https://raw.githubusercontent.com/badrulmokhtar/myvibe/main/catalog-v2.json";
        private const string CatalogV2SignatureUrl = "https://raw.githubusercontent.com/badrulmokhtar/myvibe/main/catalog-v2.json.sig";
        private const string CatalogV1Url = "https://raw.githubusercontent.com/badrulmokhtar/myvibe/main/catalog.json";
        private const string CatalogV1SignatureUrl = "https://raw.githubusercontent.com/badrulmokhtar/myvibe/main/catalog.json.sig";
        private const string CurrentManagerVersion = "0.4.0";
        private const string TrustedReleasePath = "/badrulmokhtar/myvibe/releases/download/";
        private const string CatalogV1PublicModulus = "yjcrH4sS/n+zyx4/RkZBc6WHTpzkBebMMRuHVSel+Llok5aeWJT8vCTOMsIUWf9pkMYn87tLzAO3iRTJPeF9FondJhzGqixpKzo1vhpxursq4AKbVpH8xDf39/RVkTIQOUxu7h1JlwenfAVwzVwXjngb0dV1i/16tiZHa00wllhJdhj80DM8kcp2XBmg9+wVMLS1JEQNTnhUUvkejsRTVytnvzogLNHzvkmCDEDeSzIn4j0lddeqEUbKYLSIO/A0xQhHkodgHoXym5/O0a6TC0NA0tUD43O6Hhlc9zOplliSP99dDVhAp9Kk+M1AwPRYqZHQL1qXMUpn2E16tTPhpS011ee96Rf2IFF7YhOo02RHsOlhY9U7eDPg0BGykWGZ+nDYrvGImPJNlz2faVVhhIBTzrtTcJmQVQdpyqDlHFzscCP2vHFKEZIkVT7u3ZmX2Y+Ct+fjrimelAH+weQ5aqN9Zjrx2fNs4lYb/CHceUq1blqyAzBD4nao1JUg4jxB";
        private const string CatalogV2PublicModulus = "qbFAZN9zfMnhezGb94F7uYNfOOPqCaybZ22EM4XamudRb9QSIsGxwhZ5nJCxMqtCMkzgVTV0EcWGK5sJTpMBASD8ai8ZkyFFd6HIur6jjf7JeuELtzG4QtOE6cBtcQpTZPoYDpJjhqfiRs3BnBgdFdydWJp7msFrbs0UH7yxY03fAZKKD3BDjE0+NKOjir2okTbjO8KJQjUf3mE+YrrAfN/gLfjRyYLv3NSKEDf9Mor87XnHDfulhSawp72BIlmKEI4UtFHxai2lv4ei+K8jwtID+SKxz/Xb19C3hX2L5/uIAft56FPex+1W49mTbq76kXJdgfPgqoaPQnTi28KCFCEgl+ngvkkZpoF1/A+v2Rnc25VchlFLRwWST8LA8VSt0mag2tANBeKjo1vl3lLkJmyz5S+2ddWiMTUtoZQThjSZjHRYgKAdl1DwEez9kww5ioVkrzKo5n7duooh4DMhEiY9YNLapchuksofpPxrexcdNkJgRvB8xMnSDRpxo+lJ";
        private const string CatalogPublicExponent = "AQAB";
        private const int MaxCatalogBytes = 1024 * 1024;
        private const int MaxSignatureBytes = 16 * 1024;
        private const int MaxPackageBytes = 250 * 1024 * 1024;
        internal static readonly string DownloadsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "downloads");
        private static readonly string CachedCatalogV2Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "catalog-v2.json");
        private static readonly string CachedSignatureV2Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "catalog-v2.json.sig");
        private static readonly string CachedCatalogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "catalog.json");
        private static readonly string CachedSignaturePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "catalog.json.sig");

        internal static async Task<CatalogCheckResult> CheckAsync()
        {
            try
            {
                PublicCatalog downloaded = await DownloadAndCache(CatalogV2Url, CatalogV2SignatureUrl, CachedCatalogV2Path, CachedSignatureV2Path);
                return BuildResult(downloaded, false);
            }
            catch { }
            try
            {
                return BuildResult(LoadVerified(CachedCatalogV2Path, CachedSignatureV2Path), true);
            }
            catch { }
            try
            {
                PublicCatalog legacy = await DownloadAndCache(CatalogV1Url, CatalogV1SignatureUrl, CachedCatalogPath, CachedSignaturePath);
                return BuildResult(legacy, false);
            }
            catch { }
            try
            {
                return BuildResult(LoadVerified(CachedCatalogPath, CachedSignaturePath), true);
            }
            catch { }
            return new CatalogCheckResult { Message = "Unable to verify the update catalog. The bundled 2.5D Transform installer remains available offline." };
        }

        private static async Task<PublicCatalog> DownloadAndCache(string catalogUrl, string signatureUrl, string catalogPath, string signaturePath)
        {
            byte[] catalogBytes;
            byte[] signatureBytes;
            using (WebClient client = CreateClient())
            {
                catalogBytes = await client.DownloadDataTaskAsync(new Uri(catalogUrl));
                signatureBytes = await client.DownloadDataTaskAsync(new Uri(signatureUrl));
            }
            PublicCatalog downloaded = ParseAndValidateVerified(catalogBytes, signatureBytes);
            Directory.CreateDirectory(Path.GetDirectoryName(catalogPath));
            File.WriteAllBytes(catalogPath, catalogBytes);
            File.WriteAllBytes(signaturePath, signatureBytes);
            return downloaded;
        }

        internal static async Task<OperationResult> DownloadPluginAsync(CatalogPackage package)
        {
            try
            {
                ValidatePackage(package);
                Directory.CreateDirectory(DownloadsRoot);
                string target = Path.Combine(DownloadsRoot, SafeFilePart(package.id) + "-" + SafeFilePart(package.version) + ".zip");
                string partial = target + ".partial";
                if (File.Exists(partial))
                    File.Delete(partial);
                using (WebClient client = CreateClient())
                    await client.DownloadFileTaskAsync(new Uri(package.downloadUrl), partial);
                FileInfo file = new FileInfo(partial);
                if (file.Length == 0 || file.Length > MaxPackageBytes || !VerifySha256(partial, package.sha256))
                {
                    File.Delete(partial);
                    return OperationResult.Fail("The downloaded plugin package failed integrity verification and was deleted.");
                }
                if (File.Exists(target))
                    File.Delete(target);
                File.Move(partial, target);
                return OperationResult.Ok(target + "\nVerified download");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("The plugin update could not be downloaded. " + ex.Message);
            }
        }

        internal static bool IsSafeDownloadPath(string path)
        {
            try
            {
                string root = Path.GetFullPath(DownloadsRoot) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(path);
                return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(Path.GetExtension(candidate), ".zip", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(candidate);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsSafeManagerCandidate(string path)
        {
            try
            {
                string downloads = Path.GetFullPath(DownloadsRoot) + Path.DirectorySeparatorChar;
                string versions = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "manager-versions")) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(path);
                return (candidate.StartsWith(downloads, StringComparison.OrdinalIgnoreCase) || candidate.StartsWith(versions, StringComparison.OrdinalIgnoreCase))
                    && String.Equals(Path.GetFileName(candidate), "MyVibe.exe", StringComparison.OrdinalIgnoreCase)
                    && File.Exists(candidate);
            }
            catch
            {
                return false;
            }
        }

        internal static bool VerifySha256(string path, string expected)
        {
            if (String.IsNullOrEmpty(expected) || !Regex.IsMatch(expected, "^[a-fA-F0-9]{64}$"))
                return false;
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", String.Empty);
                return String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }
        }

        internal static bool IsAuthorizedPluginPackage(string pluginId, string version, string sha256, string path)
        {
            try
            {
                PublicCatalog catalog = LoadVerifiedCachedCatalog();
                CatalogPackage package = catalog.plugins.FirstOrDefault(delegate(CatalogPackage item)
                {
                    return String.Equals(item.id, pluginId, StringComparison.OrdinalIgnoreCase)
                        && String.Equals(item.version, version, StringComparison.OrdinalIgnoreCase)
                        && String.Equals(item.sha256, sha256, StringComparison.OrdinalIgnoreCase);
                });
                return package != null && IsSafeDownloadPath(path) && VerifySha256(path, package.sha256);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsAuthorizedManagerPackage(CatalogPackage candidate, string path)
        {
            try
            {
                PublicCatalog catalog = LoadVerifiedCachedCatalog();
                CatalogPackage package = catalog.manager;
                return candidate != null
                    && package != null
                    && String.Equals(package.id, candidate.id, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(package.version, candidate.version, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(package.sha256, candidate.sha256, StringComparison.OrdinalIgnoreCase)
                    && VerifySha256(path, package.sha256);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsTrustedReleaseUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && String.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                && String.IsNullOrEmpty(uri.UserInfo)
                && uri.AbsolutePath.StartsWith(TrustedReleasePath, StringComparison.OrdinalIgnoreCase);
        }

        private static CatalogCheckResult BuildResult(PublicCatalog catalog, bool cached)
        {
            CatalogCheckResult result = new CatalogCheckResult();
            List<string> messages = new List<string>();
            foreach (PluginDefinition definition in PluginRegistry.All)
            {
                CatalogPackage package = catalog.plugins.FirstOrDefault(delegate(CatalogPackage item) { return String.Equals(item.id, definition.Id, StringComparison.OrdinalIgnoreCase); });
                if (package == null)
                    throw new InvalidDataException(definition.Name + " is missing from the catalog.");
                result.Packages[definition.Id] = package;
                string installed = PluginInstaller.GetInstalledVersion(definition);
                if (installed != null && IsNewer(package.version, installed))
                {
                    result.PluginUpdates[definition.Id] = package;
                    messages.Add(definition.Name + " " + package.version + " is ready to install.");
                }
                else
                    messages.Add(installed == null ? definition.Name + " is available." : definition.Name + " is up to date.");
            }
            result.ManagerUpdate = catalog.manager != null && IsNewer(catalog.manager.version, CurrentManagerVersion) ? catalog.manager : null;
            if (result.ManagerUpdate != null)
                messages.Add("MyVibe " + result.ManagerUpdate.version + " is available.");
            else
                messages.Add("MyVibe is up to date.");
            if (cached)
                messages.Add("Showing the last verified catalog because the network is unavailable.");
            result.Message = String.Join(" ", messages.ToArray());
            return result;
        }

        private static PublicCatalog ParseAndValidateVerified(byte[] catalogBytes, byte[] signatureBytes)
        {
            if (catalogBytes == null || catalogBytes.Length == 0 || catalogBytes.Length > MaxCatalogBytes
                || signatureBytes == null || signatureBytes.Length == 0 || signatureBytes.Length > MaxSignatureBytes)
                throw new InvalidDataException("The catalog file size is invalid.");
            string json = new System.Text.UTF8Encoding(false, true).GetString(catalogBytes);
            JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = MaxCatalogBytes };
            PublicCatalog probe = serializer.Deserialize<PublicCatalog>(json);
            if (probe == null)
                throw new InvalidDataException("The catalog structure is invalid.");
            if (probe.schemaVersion == 2)
            {
                if (!VerifyCatalogV2Signature(catalogBytes, signatureBytes))
                    throw new InvalidDataException("The catalog v2 signature is invalid.");
                return ConvertCatalogV2(serializer.Deserialize<PublicCatalogV2>(json));
            }
            if (probe.schemaVersion == 1)
            {
                if (!VerifyCatalogSignature(catalogBytes, signatureBytes))
                    throw new InvalidDataException("The catalog v1 signature is invalid.");
                if (probe.manager == null || probe.plugins == null || probe.plugins.Length == 0)
                    throw new InvalidDataException("The catalog structure is invalid.");
                ValidatePackage(probe.manager);
                foreach (CatalogPackage package in probe.plugins)
                    ValidatePackage(package);
                return probe;
            }
            throw new InvalidDataException("The catalog schema is unsupported.");
        }

        private static PublicCatalog ConvertCatalogV2(PublicCatalogV2 source)
        {
            Version managerVersion;
            if (source == null || source.schemaVersion != 2 || !String.Equals(source.channel, "beta", StringComparison.OrdinalIgnoreCase) && !String.Equals(source.channel, "stable", StringComparison.OrdinalIgnoreCase)
                || source.manager == null || !String.Equals(source.manager.id, "com.badru.myvibe", StringComparison.OrdinalIgnoreCase)
                || !Version.TryParse(source.manager.version, out managerVersion) || source.plugins == null || source.plugins.Length == 0)
                throw new InvalidDataException("The catalog v2 structure is invalid.");

            PublicCatalog converted = new PublicCatalog { schemaVersion = 2, channel = source.channel };
            converted.manager = WindowsPackage(source.manager, source.channel, false);
            List<CatalogPackage> plugins = new List<CatalogPackage>();
            HashSet<string> identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { source.manager.id };
            foreach (CatalogProductV2 product in source.plugins)
            {
                Version minimumManagerVersion;
                if (product == null || !identifiers.Add(product.id ?? String.Empty)
                    || !String.Equals(product.host, "Adobe Illustrator", StringComparison.Ordinal)
                    || !String.Equals(product.hostVersion, "30.x", StringComparison.Ordinal)
                    || !Version.TryParse(product.minimumManagerVersion, out minimumManagerVersion)
                    || IsNewer(product.minimumManagerVersion, CurrentManagerVersion))
                    throw new InvalidDataException("A catalog v2 plug-in is incompatible with this MyVibe version.");
                CatalogPackage package = WindowsPackage(product, source.channel, true);
                plugins.Add(package);
            }
            foreach (PluginDefinition definition in PluginRegistry.All)
                if (!plugins.Any(delegate(CatalogPackage package) { return String.Equals(package.id, definition.Id, StringComparison.OrdinalIgnoreCase); }))
                    throw new InvalidDataException(definition.Name + " is missing from catalog v2.");
            converted.plugins = plugins.ToArray();
            return converted;
        }

        private static CatalogPackage WindowsPackage(CatalogProductV2 product, string channel, bool required)
        {
            Version version;
            if (product == null || String.IsNullOrWhiteSpace(product.id) || String.IsNullOrWhiteSpace(product.name)
                || !Version.TryParse(product.version, out version) || product.artifacts == null)
                throw new InvalidDataException("A catalog v2 product has invalid metadata.");
            CatalogArtifactV2 artifact = product.artifacts.SingleOrDefault(delegate(CatalogArtifactV2 item)
            {
                return item != null && String.Equals(item.platform, "windows", StringComparison.OrdinalIgnoreCase)
                    && String.Equals(item.architecture, "x64", StringComparison.OrdinalIgnoreCase);
            });
            if (artifact == null)
            {
                if (required)
                    throw new InvalidDataException(product.name + " has no Windows x64 package.");
                return null;
            }
            if (artifact.signature == null || !String.Equals(artifact.signature.type, "authenticode", StringComparison.OrdinalIgnoreCase)
                || String.Equals(channel, "stable", StringComparison.OrdinalIgnoreCase) && !artifact.signature.required
                || artifact.signature.required && !Regex.IsMatch(artifact.signature.signerThumbprint ?? String.Empty, "^[a-fA-F0-9]{40,64}$"))
                throw new InvalidDataException(product.name + " has an invalid Windows signature policy.");
            CatalogPackage package = new CatalogPackage
            {
                id = product.id,
                name = product.name,
                version = product.version,
                platform = artifact.platform,
                architecture = artifact.architecture,
                downloadUrl = artifact.downloadUrl,
                sha256 = artifact.sha256,
                authenticodeRequired = artifact.signature.required,
                signerThumbprint = artifact.signature.signerThumbprint,
                host = product.host,
                hostVersion = product.hostVersion,
                minimumManagerVersion = product.minimumManagerVersion
            };
            ValidatePackage(package);
            return package;
        }

        private static PublicCatalog LoadVerifiedCachedCatalog()
        {
            try { return LoadVerified(CachedCatalogV2Path, CachedSignatureV2Path); }
            catch { return LoadVerified(CachedCatalogPath, CachedSignaturePath); }
        }

        private static PublicCatalog LoadVerified(string catalogPath, string signaturePath)
        {
            if (!File.Exists(catalogPath) || !File.Exists(signaturePath))
                throw new FileNotFoundException("A verified catalog has not been cached yet.");
            return ParseAndValidateVerified(File.ReadAllBytes(catalogPath), File.ReadAllBytes(signaturePath));
        }

        internal static bool VerifyCatalogSignature(byte[] catalogBytes, byte[] encodedSignature)
        {
            return VerifyCatalogSignatureWithKey(catalogBytes, encodedSignature, CatalogV1PublicModulus);
        }

        internal static bool VerifyCatalogV2Signature(byte[] catalogBytes, byte[] encodedSignature)
        {
            return VerifyCatalogSignatureWithKey(catalogBytes, encodedSignature, CatalogV2PublicModulus);
        }

        private static bool VerifyCatalogSignatureWithKey(byte[] catalogBytes, byte[] encodedSignature, string modulus)
        {
            try
            {
                byte[] signature = Convert.FromBase64String(System.Text.Encoding.ASCII.GetString(encodedSignature).Trim());
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(new RSAParameters
                    {
                        Modulus = Convert.FromBase64String(modulus),
                        Exponent = Convert.FromBase64String(CatalogPublicExponent)
                    });
                    return rsa.VerifyData(catalogBytes, CryptoConfig.MapNameToOID("SHA256"), signature);
                }
            }
            catch
            {
                return false;
            }
        }

        internal static bool VerifyCatalogFiles(string catalogPath, string signaturePath, out string error)
        {
            try
            {
                ParseAndValidateVerified(File.ReadAllBytes(catalogPath), File.ReadAllBytes(signaturePath));
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void ValidatePackage(CatalogPackage package)
        {
            Version parsed;
            if (package == null || !Version.TryParse(package.version, out parsed))
                throw new InvalidDataException("A catalog package has invalid metadata.");
            if (!String.Equals(package.platform, "windows", StringComparison.OrdinalIgnoreCase) || !String.Equals(package.architecture, "x64", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A catalog package targets an unsupported platform.");
            if (!Regex.IsMatch(package.sha256 ?? String.Empty, "^[a-fA-F0-9]{64}$"))
                throw new InvalidDataException("A catalog package has an invalid SHA-256 value.");
            if (!IsTrustedReleaseUrl(package.downloadUrl))
                throw new InvalidDataException("A catalog package download URL is not trusted.");
        }

        private static bool IsNewer(string candidate, string current)
        {
            Version candidateVersion;
            Version currentVersion;
            return Version.TryParse(candidate, out candidateVersion) && Version.TryParse(current, out currentVersion) && candidateVersion > currentVersion;
        }

        private static string SafeFilePart(string value)
        {
            return Regex.Replace(value ?? String.Empty, "[^A-Za-z0-9._-]", "-");
        }

        private static WebClient CreateClient()
        {
            WebClient client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "MyVibe/" + CurrentManagerVersion;
            client.Headers[HttpRequestHeader.Accept] = "application/json";
            return client;
        }
    }

    internal static class ManagerUpdates
    {
        private const long MaxManagerArchiveBytes = 100L * 1024 * 1024;
        private const long MaxManagerExtractedBytes = 350L * 1024 * 1024;
        private const int MaxManagerArchiveEntries = 2048;

        private sealed class ReplacementRequest
        {
            public string targetPath;
            public string candidatePath;
            public string currentVersion;
            public string nextVersion;
            public string healthPath;
            public int parentProcessId;
        }

        internal static async Task<OperationResult> DownloadAndStartAsync(CatalogPackage package)
        {
            try
            {
                Directory.CreateDirectory(CatalogClient.DownloadsRoot);
                string archive = Path.Combine(CatalogClient.DownloadsRoot, "MyVibe-" + package.version + ".zip");
                string partial = archive + ".partial";
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "MyVibe/0.4.0";
                    await client.DownloadFileTaskAsync(new Uri(package.downloadUrl), partial);
                }
                FileInfo managerArchive = new FileInfo(partial);
                if (managerArchive.Length == 0 || managerArchive.Length > MaxManagerArchiveBytes
                    || !CatalogClient.IsAuthorizedManagerPackage(package, partial))
                {
                    File.Delete(partial);
                    return OperationResult.Fail("The MyVibe update is not authorized by the verified catalog and was deleted.");
                }
                if (File.Exists(archive))
                    File.Delete(archive);
                File.Move(partial, archive);

                string candidateRoot = Path.Combine(CatalogClient.DownloadsRoot, "manager-" + package.version);
                if (Directory.Exists(candidateRoot))
                    Directory.Delete(candidateRoot, true);
                Directory.CreateDirectory(candidateRoot);
                ExtractManagerArchive(archive, candidateRoot);
                string candidate = Directory.GetFiles(candidateRoot, "MyVibe.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (candidate == null)
                    return OperationResult.Fail("The MyVibe release does not contain MyVibe.exe.");
                if (package.authenticodeRequired && !HasTrustedSignature(candidate, package.signerThumbprint))
                    return OperationResult.Fail("The stable MyVibe update does not have a trusted Windows signature.");

                string requestsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "update-requests");
                Directory.CreateDirectory(requestsRoot);
                string token = Guid.NewGuid().ToString("N");
                string requestPath = Path.Combine(requestsRoot, "request-" + token + ".json");
                string healthPath = Path.Combine(requestsRoot, "health-" + token + ".txt");
                ReplacementRequest request = new ReplacementRequest
                {
                    targetPath = Assembly.GetExecutingAssembly().Location,
                    candidatePath = candidate,
                    currentVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion,
                    nextVersion = package.version,
                    healthPath = healthPath,
                    parentProcessId = Process.GetCurrentProcess().Id
                };
                File.WriteAllText(requestPath, new JavaScriptSerializer().Serialize(request), new System.Text.UTF8Encoding(false));
                Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    Arguments = "--replace-manager " + Quote(requestPath),
                    UseShellExecute = true
                });
                return OperationResult.Ok("MyVibe will restart to finish the update.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("MyVibe could not start the update. " + ex.Message);
            }
        }

        internal static bool ReplaceFromRequest(string requestPath)
        {
            try
            {
                string requestRoot = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "update-requests")) + Path.DirectorySeparatorChar;
                string fullRequest = Path.GetFullPath(requestPath);
                if (!fullRequest.StartsWith(requestRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullRequest))
                    return false;
                ReplacementRequest request = new JavaScriptSerializer().Deserialize<ReplacementRequest>(File.ReadAllText(fullRequest));
                if (!CatalogClient.IsSafeManagerCandidate(request.candidatePath) || String.IsNullOrWhiteSpace(request.targetPath))
                    return false;
                try
                {
                    Process parent = Process.GetProcessById(request.parentProcessId);
                    parent.WaitForExit(20000);
                }
                catch
                {
                }

                string versionsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "manager-versions", SafeVersion(request.currentVersion));
                Directory.CreateDirectory(versionsRoot);
                string backup = Path.Combine(versionsRoot, "MyVibe.exe");
                File.Copy(request.targetPath, backup, true);
                File.Copy(request.candidatePath, request.targetPath, true);
                Process.Start(new ProcessStartInfo { FileName = request.targetPath, Arguments = "--update-health " + Quote(request.healthPath), UseShellExecute = true });

                DateTime deadline = DateTime.UtcNow.AddSeconds(25);
                while (DateTime.UtcNow < deadline && !File.Exists(request.healthPath))
                    System.Threading.Thread.Sleep(500);
                if (File.Exists(request.healthPath))
                    return true;

                File.Copy(backup, request.targetPath, true);
                Process.Start(new ProcessStartInfo { FileName = request.targetPath, UseShellExecute = true });
                return false;
            }
            catch
            {
                return false;
            }
        }

        internal static void MarkHealthy(string healthPath)
        {
            try
            {
                string root = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "update-requests")) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(healthPath);
                if (candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    File.WriteAllText(candidate, "OK");
            }
            catch
            {
            }
        }

        internal static string[] GetStoredVersions()
        {
            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "manager-versions");
            if (!Directory.Exists(root))
                return new string[0];
            return Directory.GetDirectories(root).Select(Path.GetFileName).OrderByDescending(delegate(string value) { return value; }).ToArray();
        }

        internal static OperationResult StartStoredVersion(string version)
        {
            try
            {
                string versionsRoot = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "manager-versions")) + Path.DirectorySeparatorChar;
                string candidate = Path.GetFullPath(Path.Combine(versionsRoot, version, "MyVibe.exe"));
                if (!candidate.StartsWith(versionsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate))
                    return OperationResult.Fail("The stored MyVibe version is no longer available.");
                string requestsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyVibe", "update-requests");
                Directory.CreateDirectory(requestsRoot);
                string token = Guid.NewGuid().ToString("N");
                string requestPath = Path.Combine(requestsRoot, "request-" + token + ".json");
                ReplacementRequest request = new ReplacementRequest
                {
                    targetPath = Assembly.GetExecutingAssembly().Location,
                    candidatePath = candidate,
                    currentVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion,
                    nextVersion = version,
                    healthPath = Path.Combine(requestsRoot, "health-" + token + ".txt"),
                    parentProcessId = Process.GetCurrentProcess().Id
                };
                File.WriteAllText(requestPath, new JavaScriptSerializer().Serialize(request), new System.Text.UTF8Encoding(false));
                Process.Start(new ProcessStartInfo { FileName = candidate, Arguments = "--replace-manager " + Quote(requestPath), UseShellExecute = true });
                return OperationResult.Ok("MyVibe will restart with version " + version + ".");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("MyVibe could not start the rollback. " + ex.Message);
            }
        }

        internal static bool IsSafeArchiveEntryName(string name)
        {
            if (String.IsNullOrWhiteSpace(name) || Path.IsPathRooted(name) || name.IndexOf(':') >= 0)
                return false;
            string[] parts = name.Replace('\\', '/').Split('/');
            return parts.All(delegate(string part) { return part != ".."; });
        }

        private static void ExtractManagerArchive(string archivePath, string destination)
        {
            string root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
            long extractedBytes = 0;
            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
            {
                if (archive.Entries.Count > MaxManagerArchiveEntries)
                    throw new InvalidDataException("The MyVibe release contains too many files.");
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!IsSafeArchiveEntryName(entry.FullName) || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                        throw new InvalidDataException("The MyVibe release contains an unsafe archive entry.");
                    extractedBytes += entry.Length;
                    if (extractedBytes > MaxManagerExtractedBytes)
                        throw new InvalidDataException("The MyVibe release is too large after extraction.");
                    string target = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
                    if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The MyVibe release contains an unsafe path.");
                    if (String.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(target);
                        continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                }
            }
        }

        private static bool HasTrustedSignature(string file, string expectedThumbprint)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(expectedThumbprint))
                    return false;
                X509Certificate certificate = X509Certificate.CreateFromSignedFile(file);
                using (X509Certificate2 signer = new X509Certificate2(certificate))
                using (X509Chain chain = new X509Chain())
                    return chain.Build(signer)
                        && String.Equals(NormalizeThumbprint(signer.Thumbprint), NormalizeThumbprint(expectedThumbprint), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeThumbprint(string value)
        {
            return Regex.Replace(value ?? String.Empty, "[^A-Fa-f0-9]", String.Empty);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string SafeVersion(string value)
        {
            return Regex.Replace(value ?? "unknown", "[^0-9A-Za-z._-]", "-");
        }
    }
}
