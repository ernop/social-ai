
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
namespace SocialAi
{
    public class FileManager
    {
        //line-height, adjusted up to make it more visually appealing
        public static int LineSize { get; set; } = 45;
        public static int FontSize { get; set; } = 36;

        //Unused currently, but we should move text in a bit to make it more visible in twitter previews where now it's slightly cut off.
        public static int HorizontalBuffer { get; set; } = 10;

        //extra y to add to images in annotation section as a kind of vertical buffer.
        public static int TextExtraY { get; set; } = LineSize / 2 + 5;
        public Font Font { get; set; } = new Font("Gotham", FontSize, FontStyle.Regular);
        public JsonSettings Settings { get; set; }

        // A fake object required to calculate text widths.
        public Graphics FakeGraphics { get; set; }

        public void Init(JsonSettings settings)
        {
            Settings = settings;

            var fakePath = settings.ProjBase + "/image.png";
            if (!System.IO.File.Exists(fakePath))
            {
                throw new Exception("something is wrong with your project settings file; you need to define projbase so projbase/image.png hits something");
            }
            FakeGraphics = Graphics.FromImage(new Bitmap(fakePath));
        }

        public List<string> GetTextInLines(string? text, int pixelWidth)
        {
            //for some reason we need a  "real" graphics object to calculate text widths based off of.

            var remainingText = text + " ";

            var lines = new List<string>();
            while (remainingText != "")
            {
                if (remainingText == " ")
                {
                    break;
                }
                var testLength = remainingText.Length - 1;
                while (true)
                {
                    if (testLength == 0)
                    {
                        break;
                    }
                    var nth = remainingText[testLength];
                    if (nth != ' ')
                    {
                        testLength--;
                        continue;
                    }
                    var candidateText = remainingText.Substring(0, testLength);

                    var w = FakeGraphics.MeasureString(candidateText, Font);
                    if (w.Width < pixelWidth)
                    {
                        remainingText = remainingText.Substring(testLength);
                        lines.Add(candidateText.Trim());
                        break;
                    }
                    testLength--;

                }
            }

            return lines;
        }


        //combine with outer method but whatever.
        private async Task<Stream> GetImageByteStreamAsync(string url)
        {
            var uri = new Uri(url);
            using var httpClient = new HttpClient();

            var stream = await httpClient.GetStreamAsync(uri);
            return stream;
        }

        /// <summary>
        /// resize proportionally.
        /// </summary>
        public Image ResizeImage(Image im, int newMaxHeightPixels)
        {
            var ratio = (double)newMaxHeightPixels / im.Height;
            var newWidth = (int)(im.Width * ratio);
            var newHeight = (int)(im.Height * ratio);
            var newImage = new Bitmap(newWidth, newHeight);
            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(im, 0, 0, newWidth, newHeight);
            }
            im.Dispose();
            return newImage;
        }

        public async Task<string> Annotate(string fp, Prompt prompt)
        {
            var text = prompt.GetAnnotation();

            var outputImageToAnnotate = Image.FromFile(fp);
            var holdImages = new List<Image>();
            Size outputSize;
            //output is the original size, plus some Y increase to fit the text.
            //plus, if the image were constructed from other images through "blend" or similar
            //expand the X axis much more to fit them in too.

            var outputOffsetX = 0;

            var maxYSeen = outputImageToAnnotate.Height;

            //note that we jam the images together tightly. 
            //investigate adding small "+" between them and a final "=" plus some extra width, possibly.
            if (prompt.ReferencedImages != null && prompt.ReferencedImages.Count > 0)
            {
                //download them, and figure out their sizes.
                foreach (var imgUrl in prompt.ReferencedImages)
                {
                    var stream = await GetImageByteStreamAsync(imgUrl);
                    Image refImage;
                    try
                    {
                        refImage = Image.FromStream(stream);
                    }
                    catch (Exception ex)
                    {
                        //this image will simply fail.
                        continue;
                    }

                    if (refImage.Height > 1024)
                    {
                        refImage = ResizeImage(refImage, 1024);
                    }

                    holdImages.Add(refImage);
                    outputOffsetX += refImage.Width;
                    maxYSeen = Math.Max(maxYSeen, refImage.Height);

                }
                outputSize = new Size(outputOffsetX + outputImageToAnnotate.Size.Width, maxYSeen);
            }
            else
            {
                outputSize = outputImageToAnnotate.Size;
            }

            var lines = GetTextInLines(text, outputSize.Width);

            var extraYPixels = LineSize * lines.Count() + TextExtraY;

            var im = new Bitmap(outputSize.Width, outputSize.Height + extraYPixels);

            var graphics = Graphics.FromImage(im);
            graphics.Clear(Color.Black);

            var holdOffsetX = 0;
            foreach (var holdImage in holdImages)
            {
                graphics.DrawImage(holdImage, new Point(holdOffsetX, 0));
                holdOffsetX += holdImage.Width;
            }

            graphics.DrawImage(outputImageToAnnotate, new Point(outputOffsetX, 0));

            outputImageToAnnotate.Dispose();

            var ii = 0;
            var brush = new SolidBrush(Color.White);

            //note that we draw the text fully left-aligned which may be weird for blend images.
            foreach (var line in lines)
            {
                var pos = (float)Math.Floor((double)(maxYSeen + TextExtraY / 2 + ii * LineSize));
                ii += 1;
                graphics.DrawString(line, Font, brush, new PointF(0, pos));
            }
            graphics.Save();
            im.Save(fp);
            Console.WriteLine($"Successfully saved new fp: {fp}");
            return fp;
        }

        /// <summary>
        /// mosaic = it's a mosaic image, so we don't want to save it to the same folder as the originals.
        /// </summary>
        public string? GetPathToSave(string filename, bool mosaic = false)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("missing filename");
            }

            var mosText = "";
            if (mosaic)
            {
                mosText = "mosaic/";
            }

            var joined = $"{Settings.ImageOutputFullPath}/{mosText}{filename}";
            if (File.Exists(joined))
            {
                Console.WriteLine($"Skipping existing: {joined}");
                return null;
            }

            //also exclude downloading if it exists in the backup folder
            var joinedCLeaned = $"{Settings.CleanedImageOutputFullPath}/{filename}";
            if (File.Exists(joinedCLeaned))
            {
                Console.WriteLine($"Skipping existing cleaned: {joinedCLeaned}");
                return null;
            }

            Console.WriteLine($"Will download: {joined}");
            return joined;
        }
    }
}