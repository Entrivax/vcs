using FFMpegCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
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
                var reusableThumbnail = thumbnailsCache?.FirstOrDefault(thumbnail => thumbnail.TimeSpan == timestamp && thumbnail.Image.Width == config.Width);
                if (reusableThumbnail != null)
                {
                    images[i] = reusableThumbnail;
                    continue;
                }
                images[i] = new Thumbnail(timestamp, (await FFMpeg.SnapshotAsync(config.FileName, new System.Drawing.Size(config.Width, 0), timestamp)).ToImageSharpImage<Rgba32>());

            }
            progress?.Report(new ImagesExtractionProgressReport(imagesCount, imagesCount));

            return images;
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
