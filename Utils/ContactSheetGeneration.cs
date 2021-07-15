using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCS.Models;
using SixLabors.Fonts;
using Vurdalakov;

namespace VCS.Utils
{
    public class ContactSheetGeneration
    {
        public static Task<Image<Rgba32>> Generate(FileConfig file, TimeSpan[] timestamps, Image?[] thumbnails, IReadOnlyFontCollection fontCollection, IProgress<ContactSheetGenerationProgressReport>? progress = null)
        {
            if (file.MediaInfo == null)
            {
                throw new Exception("FileConfig is not loaded");
            }
            var firstValidThumbnail = thumbnails.First((image) => image != null);
            if (firstValidThumbnail == null)
            {
                throw new Exception("Unable to find a thumbnail");
            }
            return Task.Run(() =>
            {
                var thumbnailsWidth = firstValidThumbnail.Width;
                var thumbnailsHeight = firstValidThumbnail.Height;
                var width = (thumbnailsWidth + file.BorderSize * 2) * file.Columns + file.Padding * (file.Columns + 1);
                var height = (thumbnailsHeight + file.BorderSize * 2) * file.Rows + file.Padding * (file.Rows + 1);
                var image = new Image<Rgba32>(width, height);
                image.Mutate(x =>
                {
                    var font = fontCollection.Find("Fira Code").CreateFont(13f, FontStyle.Bold);
                    x.Fill(Color.FromRgb(45, 45, 45));

                    var timestampDrawingOptions = new DrawingOptions()
                    {
                        TextOptions = new TextOptions
                        {
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Bottom
                        }
                    };
                    var fileInfoDrawingOptions = new DrawingOptions()
                    {
                        TextOptions = new TextOptions
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top
                        }
                    };
                    for (var i = 0; i < thumbnails.Length; i++)
                    {
                        progress?.Report(new ContactSheetGenerationProgressReport(thumbnails.Length, i));
                        var column = i % file.Columns;
                        var row = (i / file.Columns);
                        var imagePosition = new PointF(
                            column * (file.Padding + thumbnailsWidth + file.BorderSize * 2) + file.Padding + file.BorderSize,
                            row * (file.Padding + thumbnailsHeight + file.BorderSize * 2) + file.Padding + file.BorderSize
                        );
                        var timestampText = TimestampUtils.SecondsToTimestamp((float)timestamps[i].TotalSeconds, true, timestamps[timestamps.Length - 1].TotalHours > 0, timestamps[timestamps.Length - 1].TotalMinutes > 0);

                        x
                            .Fill(Color.FromRgb(194, 190, 197), new RectangleF(imagePosition - new PointF(file.BorderSize, file.BorderSize), new SizeF(thumbnailsWidth + file.BorderSize * 2, thumbnailsHeight + file.BorderSize * 2)))
                            .Fill(Color.Black, new RectangleF(imagePosition, new SizeF(thumbnailsWidth, thumbnailsHeight)));
                        if (thumbnails[i] != null)
                        {
                            x.DrawImage(thumbnails[i], (Point)imagePosition, 1f);
                        }
                        x
                            .DrawText(timestampDrawingOptions, timestampText, font, Color.Black, new PointF(imagePosition.X + thumbnailsWidth - file.BorderSize - 5, imagePosition.Y + thumbnailsHeight - file.BorderSize - 5))
                            .DrawText(timestampDrawingOptions, timestampText, font, Color.White, new PointF(imagePosition.X + thumbnailsWidth - file.BorderSize - 5 - 1, imagePosition.Y + thumbnailsHeight - file.BorderSize - 5 - 1));
                    }
                    progress?.Report(new ContactSheetGenerationProgressReport(thumbnails.Length, thumbnails.Length));
                    var codecs = new List<string>();
                    if (file.MediaInfo.PrimaryVideoStream != null)
                    {
                        codecs.Add(file.MediaInfo.PrimaryVideoStream.CodecName);
                    }
                    if (file.MediaInfo.PrimaryAudioStream != null)
                    {
                        codecs.Add(file.MediaInfo.PrimaryAudioStream.CodecName);
                    }
                    var fileInfoText = $"{file}\n" +
                    $"{TimestampUtils.SecondsToTimestamp((float)file.MediaInfo.Duration.TotalSeconds, true)}, {FileSizeUtils.GetFileSizeString((long)((file.MediaInfo.Format.BitRate * file.MediaInfo.Format.Duration.TotalSeconds) / 8))}\n" +
                    $"{codecs.Aggregate((a, b) => a + ", " + b)}\n" +
                    (file.MediaInfo.PrimaryVideoStream != null ? $"{file.MediaInfo.PrimaryVideoStream.Width} x {file.MediaInfo.PrimaryVideoStream.Height}, {String.Format("{0:0.#}", file.MediaInfo.PrimaryVideoStream.FrameRate)} fps" : "");
                    x
                        .DrawText(fileInfoDrawingOptions, fileInfoText, font, Color.Black, new PointF(file.Padding + file.BorderSize + 5 + 1, file.Padding + file.BorderSize + 5 + 1))
                        .DrawText(fileInfoDrawingOptions, fileInfoText, font, Color.White, new PointF(file.Padding + file.BorderSize + 5, file.Padding + file.BorderSize + 5));
                });

                return image;
            });
        }

        public class ContactSheetGenerationProgressReport
        {
            public int ImagesCount { get; }
            public int ProcessedImages { get; }
            public ContactSheetGenerationProgressReport(int imagesCount, int processedImages)
            {
                ImagesCount = imagesCount;
                ProcessedImages = processedImages;
            }
        }
    }
}
