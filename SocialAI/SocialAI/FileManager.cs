
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


        public void Annotate(string fp, string? text)
        {
            var originalImage = Image.FromFile(fp);
            var originalSize = originalImage.Size;
            var fakeGraphics = Graphics.FromImage(originalImage);
          
            var lines = GetTextInLines(text, originalSize.Width);
            fakeGraphics.Dispose();

            var extraYPixels = LineSize * lines.Count() + TextExtraY;

            var im = new Bitmap(originalSize.Width, originalSize.Height + extraYPixels);

            var graphics = Graphics.FromImage(im);
            graphics.Clear(Color.Black);
            graphics.DrawImage(originalImage, new Point(0, 0));

            originalImage.Dispose();

            var ii = 0;
            var brush = new SolidBrush(Color.White);

            foreach (var line in lines)
            {
                var pos = (float)Math.Floor((double)(originalSize.Height + TextExtraY / 2 + ii * LineSize));
                ii += 1;
                graphics.DrawString(line, Font, brush, new PointF(0, pos));
            }
            graphics.Save();
            im.Save(fp);
        }

        public string? GetPathToSave(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("missing filename");
            }

            var joined = $"{Settings.ImageOutputFullPath}/{filename}";
            if (File.Exists(joined))
            {
                return null;
            }

            return joined;
        }
    }
}