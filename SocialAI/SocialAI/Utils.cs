
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

public class FileManager
{
    public string ImageFolderFullPath { get; set; }
    public static int LineSize { get; set; } = 45;
    public static int FontSize { get; set; } = 36;
    public static int TextExtraY { get; set; } = LineSize / 2+5;
    public Font Font { get; set; } = new Font("Gotham", FontSize, FontStyle.Regular);


    public FileManager(JsonSettings settings)
    {
        ImageFolderFullPath = settings.ImageBase;
    }

    
    public List<string> GetTextInLines(string text,int pixelWidth, Graphics g)
    {
        var remainingText = text+" ";
        
        var lines = new List<string>();
        while (remainingText != "")
        {
            if (remainingText==" ")
            {
                break;
            }
            var testLength = remainingText.Length-1;
            while (true)
            {
                if (testLength == 0)
                {
                    break;
                }
                var nth = remainingText[testLength];
                if (nth!=' ')
                {
                    testLength--;
                    continue;
                }
                var candidateText = remainingText.Substring(0, testLength);
                var w = g.MeasureString(candidateText, Font);
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


    public void Annotate(string fp, string text)
    {
        var originalImage = Image.FromFile(fp);
        var originalSize = originalImage.Size;
        var fakeGraphics = Graphics.FromImage(originalImage);

        var lines = GetTextInLines(text, originalSize.Width, fakeGraphics);
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
        var x = 5;
    }

    public string GetPathToSave(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentNullException();
        }
        
        while (true)
        {
            var joined = $"{ImageFolderFullPath}/{filename}";
            if (File.Exists(joined))
            {
                break;
            }

            return joined;
        }
        return "";
    }
}
