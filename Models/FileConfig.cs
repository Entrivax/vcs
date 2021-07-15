using FFMpegCore;
using ReactiveUI;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VCS.Models
{
    public class FileConfig : ReactiveObject
    {
        string fileName;
        public string FileName
        {
            get => fileName;
            set
            {
                this.RaiseAndSetIfChanged(ref fileName, value);
                TimestampsValidated = false;
                ThumbnailsValidated = false;
            }
        }

        int columns;
        public int Columns
        {
            get => columns;
            set
            {
                this.RaiseAndSetIfChanged(ref columns, value);
                TimestampsValidated = false;
                ThumbnailsValidated = false;
            }
        }
        int rows;
        public int Rows
        {
            get => rows;
            set
            {
                this.RaiseAndSetIfChanged(ref rows, value);
                TimestampsValidated = false;
                ThumbnailsValidated = false;
            }
        }
        int padding;
        public int Padding
        {
            get => padding;
            set => this.RaiseAndSetIfChanged(ref padding, value);
        }
        int borderSize;
        public int BorderSize
        {
            get => borderSize;
            set => this.RaiseAndSetIfChanged(ref borderSize, value);
        }
        int width;
        public int Width
        {
            get => width;
            set
            {
                this.RaiseAndSetIfChanged(ref width, value);
                ThumbnailsValidated = false;
            }
        }

        TimeSpan[]? timestamps;
        [JsonIgnore]
        public TimeSpan[]? Timestamps {
            get => timestamps;
            set
            {
                timestamps = value;
                TimestampsValidated = true;
            }
        }
        [JsonIgnore]
        public bool TimestampsValidated { get; private set; } = false;
        Thumbnail[]? thumbnails;
        [JsonIgnore]
        public Thumbnail[]? Thumbnails
        {
            get => thumbnails;
            set
            {
                thumbnails = value;
                ThumbnailsValidated = true;
            }
        }
        [JsonIgnore]
        public bool ThumbnailsValidated { get; private set; } = false;
        [JsonIgnore]
        public IMediaAnalysis? MediaInfo { get; private set; }

        public ObservableCollection<Timestamp> Highlights { get; set; } = new ObservableCollection<Timestamp>();

        public FileConfig() : this("", null) { }
        public FileConfig(string fileName, FileConfig? defaultValues = null)
        {
            this.fileName = fileName;
            if (defaultValues != null)
            {
                this.Columns = defaultValues.Columns;
                this.Rows = defaultValues.Rows;
                this.Padding = defaultValues.Padding;
                this.Width = defaultValues.Width;
                this.BorderSize = defaultValues.BorderSize;
            }

            Highlights.CollectionChanged += Highlights_CollectionChanged;
        }

        private void Highlights_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TimestampsValidated = false;
            ThumbnailsValidated = false;
        }

        public async Task LoadMedia()
        {
            this.MediaInfo = await FFProbe.AnalyseAsync(this.FileName);
        }

        public override string ToString()
        {
            return Path.GetFileName(FileName);
        }
    }
}
