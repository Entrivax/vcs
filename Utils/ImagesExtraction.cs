using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VCS.Models;
using Vurdalakov;

namespace VCS.Utils
{
    public class ImagesExtraction
    {
        public static async Task<Thumbnail[]> ExtractImages(FileConfig config, TimeSpan[] timestamps, Thumbnail[]? thumbnailsCache, IProgress<ImagesExtractionProgressReport>? progress = null)
        {
            var imagesCount = config.Columns * config.Rows;
            Thumbnail[] images = new Thumbnail[imagesCount];
            for (var i = 0; i < imagesCount; i++)
            {
                progress?.Report(new ImagesExtractionProgressReport(imagesCount, i));
                var timestamp = timestamps[i];
                var reusableThumbnail = thumbnailsCache?.FirstOrDefault(thumbnail => thumbnail.TimeSpan == timestamp && thumbnail.Image?.Width == config.Width);
                if (reusableThumbnail != null)
                {
                    images[i] = reusableThumbnail;
                    continue;
                }
                images[i] = new Thumbnail(timestamp, (await SnapshotAsync(config, new System.Drawing.Size(config.Width, 0), timestamp))?.ToImageSharpImage<Rgba32>());

            }
            progress?.Report(new ImagesExtractionProgressReport(imagesCount, imagesCount));

            return images;
        }

        // Methods modified from FFMpegCore to reuse the same IMediaAnalysis between each thumbnail of the same file + return null when the output is empty
        public static async Task<Bitmap?> SnapshotAsync(FileConfig config, System.Drawing.Size? size = null, TimeSpan? captureTime = null)
        {
            IMediaAnalysis source = config.MediaInfo != null ? config.MediaInfo : await FFProbe.AnalyseAsync(config.FileName);
            var (arguments, outputOptions) = BuildSnapshotArguments(config.FileName, source, size, captureTime);
            using var ms = new MemoryStream();

            await arguments
                .OutputToPipe(new StreamPipeSink(ms), options => outputOptions(options
                    .ForceFormat("rawvideo")))
                .ProcessAsynchronously();

            ms.Position = 0;
            try
            {
                return new Bitmap(ms);
            }
            catch(Exception e)
            {
                return null;
            }
        }

        private static (FFMpegArguments, Action<FFMpegArgumentOptions> outputOptions) BuildSnapshotArguments(string input, IMediaAnalysis source, System.Drawing.Size? size = null, TimeSpan? captureTime = null)
        {
            captureTime ??= TimeSpan.FromSeconds(source.Duration.TotalSeconds / 3);
            size = PrepareSnapshotSize(source, size);

            return (FFMpegArguments
                .FromFileInput(input, false, options => options
                    .Seek(captureTime)),
                options => options
                    .WithVideoCodec(VideoCodec.Png)
                    .WithFrameOutputCount(1)
                    .Resize(size));
        }

        private static System.Drawing.Size? PrepareSnapshotSize(IMediaAnalysis source, System.Drawing.Size? wantedSize)
        {
            if (wantedSize == null || (wantedSize.Value.Height <= 0 && wantedSize.Value.Width <= 0) || source.PrimaryVideoStream == null)
                return null;

            var currentSize = new System.Drawing.Size(source.PrimaryVideoStream.Width, source.PrimaryVideoStream.Height);
            if (source.PrimaryVideoStream.Rotation == 90 || source.PrimaryVideoStream.Rotation == 180)
                currentSize = new System.Drawing.Size(source.PrimaryVideoStream.Height, source.PrimaryVideoStream.Width);

            if (wantedSize.Value.Width != currentSize.Width || wantedSize.Value.Height != currentSize.Height)
            {
                if (wantedSize.Value.Width <= 0 && wantedSize.Value.Height > 0)
                {
                    var ratio = (double)wantedSize.Value.Height / currentSize.Height;
                    return new System.Drawing.Size((int)(currentSize.Width * ratio), (int)(currentSize.Height * ratio));
                }
                if (wantedSize.Value.Height <= 0 && wantedSize.Value.Width > 0)
                {
                    var ratio = (double)wantedSize.Value.Width / currentSize.Width;
                    return new System.Drawing.Size((int)(currentSize.Width * ratio), (int)(currentSize.Height * ratio));
                }
                return wantedSize;
            }

            return null;
        }

        public class ImagesExtractionProgressReport
        {
            public int ImagesCount { get; }
            public int ProcessedImages { get; }
            public ImagesExtractionProgressReport(int imagesCount, int processedImages)
            {
                ImagesCount = imagesCount;
                ProcessedImages = processedImages;
            }
        }
    }
}
