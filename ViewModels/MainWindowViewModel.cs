using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VCS.Models;
using VCS.Utils;
using VCS.Views;
using Vurdalakov;

namespace VCS.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        public ObservableCollection<FileConfig> Files { get; } = new ObservableCollection<FileConfig>();

        FileConfig? selectedFile = null;
        public FileConfig? SelectedFile
        {
            get => selectedFile;
            set
            {
                this.RaiseAndSetIfChanged(ref selectedFile, value);
                CurrentConfig = selectedFile != null ? selectedFile : defaultConfig;
            }
        }

        FileConfig? currentConfig;
        public FileConfig? CurrentConfig
        {
            get => currentConfig;
            set => this.RaiseAndSetIfChanged(ref currentConfig, value);
        }

        Avalonia.Media.Imaging.Bitmap? previewImage;
        public Avalonia.Media.Imaging.Bitmap? PreviewImage
        {
            get => previewImage;
            set => this.RaiseAndSetIfChanged(ref previewImage, value);
        }

        FileConfig defaultConfig = new FileConfig("");

        public SelectionModel<Timestamp> HighlightsSelection { get; }

        bool highlightSelected;
        public bool HighlightSelected
        {
            get => highlightSelected;
            set => this.RaiseAndSetIfChanged(ref highlightSelected, value);
        }

        int progressValue = 0;
        public int ProgressValue
        {
            get => progressValue;
            set => this.RaiseAndSetIfChanged(ref progressValue, value);
        }

        int progressMax = 1;
        public int ProgressMax
        {
            get => progressMax;
            set => this.RaiseAndSetIfChanged(ref progressMax, value);
        }

        int serverPort = 33321;
        public int ServerPort
        {
            get => serverPort;
            set => this.RaiseAndSetIfChanged(ref serverPort, value);
        }

        public string ToggleServerText
        {
            get => remoteAccess != null ? "Stop server" : "Start server";
        }

        System.Net.Http.HttpListener? remoteAccess;

        public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

        public ReactiveCommand<Unit, Unit> AddHighlight { get; }

        public ReactiveCommand<Unit, Unit> UpdatePreview { get; }

        public ReactiveCommand<Unit, Unit> ProcessFilesCommand { get; }

        public ReactiveCommand<Unit, Unit> RemoveHighlights { get; }

        public ReactiveCommand<Unit, Unit> DeleteSelectedFile { get; }

        public ReactiveCommand<Unit, Unit> ClearFiles { get; }

        public ReactiveCommand<Unit, Unit> ToggleServer { get; }

        public MainWindowViewModel()
        {
            defaultConfig.Columns = 8;
            defaultConfig.Rows = 8;
            defaultConfig.Padding = 5;
            defaultConfig.Width = 240;
            defaultConfig.BorderSize = 1;
            HighlightsSelection = new SelectionModel<Timestamp>();
            HighlightsSelection.SelectionChanged += HighlightsSelection_SelectionChanged;
            OpenFileCommand = ReactiveCommand.CreateFromTask(async () => {
                var result = await new OpenFileDialog()
                {
                    AllowMultiple = true,
                    Filters = new List<FileDialogFilter>()
                    {
                        new FileDialogFilter
                        {
                            Name = "Video file",
                            Extensions = new List<string>
                            {
                                "flv",
                                "mkv",
                                "mp4",
                                "webm",
                                "wmv",
                            }
                        }
                    }
                }.ShowAsync(MainWindow.Instance);
                for (var i = 0; i < result.Length; i++)
                {
                    try
                    {
                        await LoadFile(result[i], defaultConfig);
                    }
                    catch { }
                }
                await SaveSession();
            });

            ShowNewHighlightDialog = new Interaction<Unit, float?>();

            AddHighlight = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await ShowNewHighlightDialog.Handle(Unit.Default);
                if (result != null)
                {
                    if (selectedFile != null)
                    {
                        selectedFile.Highlights.Add(new Timestamp(result.Value));
                        await SaveSession();
                    }
                }
            });

            UpdatePreview = ReactiveCommand.CreateFromTask(async () =>
            {
                var file = SelectedFile;
                if (file == null)
                {
                    return;
                }
                PreviewImage = (await GenerateContactSheet(file)).ToAvaloniaBitmap();
            });

            ProcessFilesCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var files = Files.ToList();
                foreach (var file in files)
                {
                    if (file == null)
                    {
                        continue;
                    }
                    var image = await GenerateContactSheet(file);
                    using (var stream = File.OpenWrite(Path.Combine(Path.GetDirectoryName(file.FileName), Path.GetFileNameWithoutExtension(file.FileName)) + ".png"))
                    {
                        await image.SaveAsync(stream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                    }
                }
            });

            ClearFiles = ReactiveCommand.CreateFromTask(async () =>
            {
                Files.Clear();
                await SaveSession();
            });

            DeleteSelectedFile = ReactiveCommand.CreateFromTask(async () =>
            {
                if (SelectedFile != null)
                {
                    var indexOf = Files.IndexOf(SelectedFile);
                    Files.Remove(SelectedFile);
                    await SaveSession();
                    if (Files.Count == 0)
                    {
                        SelectedFile = null;
                        return;
                    }
                }
            });

            RemoveHighlights = ReactiveCommand.CreateFromTask(async () =>
            {
                HighlightsSelection.SelectedItems.ToList().ForEach((highlight) =>
                {
                    CurrentConfig?.Highlights.Remove(highlight);
                });
                await SaveSession();
            });

            ToggleServer = ReactiveCommand.Create(() =>
            {
                if (remoteAccess != null)
                {
                    remoteAccess.Close();
                    remoteAccess = null;
                }
                else
                {
                    StartWebServer();
                }
                this.RaisePropertyChanged("ToggleServerText");
            });

        }

        public async Task Load()
        {
            var sessionData = await CurrentSessionManager.Load();
            if (sessionData != null)
            {
                if (sessionData.Files != null)
                {
                    for (var i = 0; i < sessionData.Files.Length; i++)
                    {
                        try
                        {
                            await sessionData.Files[i].LoadMedia();
                            Files.Add(sessionData.Files[i]);
                        }
                        catch { }
                    }
                }
            }
        }

        public async Task OnFocusLost(object sender, RoutedEventArgs e)
        {
            await SaveSession();
        }

        private async Task SaveSession()
        {
            await CurrentSessionManager.Save(new SessionData
            {
                Files = Files.ToArray()
            });
        }

        private void HighlightsSelection_SelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs<Timestamp> e)
        {
            HighlightSelected = HighlightsSelection.SelectedItems != null && HighlightsSelection.SelectedItems.Count > 0;
        }

        public Interaction<Unit, float?> ShowNewHighlightDialog { get; }

        public async Task<SixLabors.ImageSharp.Image<Rgba32>> GenerateContactSheet(FileConfig file)
        {
            if (!file.TimestampsValidated || file.Timestamps == null)
            {
                file.Timestamps = TimestampUtils.GetThumbnailsTimestamps(file);
            }
            if (!file.ThumbnailsValidated || file.Thumbnails == null)
            {
                file.Thumbnails = await ImagesExtraction.ExtractImages(file, file.Timestamps, file.Thumbnails, new Progress<ImagesExtraction.ImagesExtractionProgressReport>((report) =>
                {
                    ProgressValue = report.ProcessedImages;
                    ProgressMax = report.ImagesCount;
                }));
            }
            return await ContactSheetGeneration.Generate(file, file.Timestamps, file.Thumbnails.Select(thumbnail => thumbnail.Image).ToArray(), SixLabors.Fonts.SystemFonts.Collection, new Progress<ContactSheetGeneration.ContactSheetGenerationProgressReport>(report =>
            {
                ProgressValue = report.ProcessedImages;
                ProgressMax = report.ImagesCount;
            }));
        }

        private async Task<FileConfig> LoadFile(string fileName, FileConfig defaultConfig)
        {
            var fileConfig = new FileConfig(fileName, defaultConfig);
            await fileConfig.LoadMedia();
            Files.Add(fileConfig);
            return fileConfig;
        }

        private void StartWebServer()
        {
            remoteAccess = new System.Net.Http.HttpListener(IPAddress.Parse("127.0.0.1"), 33321);

            try
            {
                remoteAccess.Request += async (sender, context) =>
                {
                    try
                    {
                        var request = context.Request;
                        var response = context.Response;
                        if (request.HttpMethod == HttpMethods.Post)
                        {
                            try
                            {
                                var str = await request.ReadContentAsStringAsync();
                                var data = JsonSerializer.Deserialize<RemoteAccessData>(str);
                                if (data == null || data.FileName == null)
                                {
                                    response.Close();
                                    return;
                                }
                                var normalizedPath = Path.GetFullPath(data.FileName);

                                var file = Files.FirstOrDefault(config => Path.GetFullPath(config.FileName) == normalizedPath) ?? await LoadFile(data.FileName, defaultConfig);
                                if (data.Timestamp != null)
                                {
                                    file.Highlights.Add(new Timestamp((float)data.Timestamp));
                                }
                                await SaveSession();
                                await response.WriteContentAsync("Ok");
                            }
                            catch
                            {
                                response.Close();
                                return;
                            }
                        }
                        else
                        {
                            response.MethodNotAllowed();
                        }
                        // Close the HttpResponse to send it back to the client.
                        response.Close();
                    }
                    catch { }
                };

                remoteAccess.Start();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                remoteAccess.Close();
                remoteAccess = null;
            }
        }

        private class RemoteAccessData
        {
            public string? FileName { get; set; }
            public float? Timestamp { get; set; }
        }
    }
}
