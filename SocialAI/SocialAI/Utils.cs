
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

public class FileManager

{
    public string ImageStorage { get; set; }
    public static int LineSize { get; set; } = 55;
    public static int FontSize { get; set; } = 48;
    public static int CharsPerLine { get; set; } = 40;
    public static int TextExtraY { get; set; } = LineSize/2;

    public FileManager(string imageStorage)
    {
        ImageStorage = imageStorage;
    }

    public Font Arial { get; set; } = new Font("Gotham", FontSize, FontStyle.Regular);

    public string GetTextInLines(string text, int lineLength)
    {
        var lines = new List<string>();
        var line = "";
        foreach (var w in text.Split(' '))
        {
            if (line.Length + w.Length > lineLength)
            {
                lines.Add(line);
                line = w;
            }
            else
            {
                if (line == "")
                {
                    line = w;
                }
                else
                {
                    line = line + " " + w;
                }
            }
        }
        if (!string.IsNullOrEmpty(line))
        {
            lines.Add(line);
        }
        return String.Join('\n', lines);
    }


    public void Annotate(string fp, string text)
    {
        var g = Image.FromFile(fp);
        var s = g.Size;
        var lines = GetTextInLines(text, CharsPerLine);

        var extra = LineSize * lines.Split('\n').Count() + TextExtraY;

        var im = new Bitmap(s.Width, s.Height + extra);

        var graphics = Graphics.FromImage(im);
        graphics.Clear(Color.Black);
        graphics.DrawImage(g, new Point(0, 0));

        g.Dispose();

        var ii = 0;
        var brush = new SolidBrush(Color.White);

        foreach (var line in lines.Split('\n'))
        {
            var pos = (float)Math.Floor((double)(s.Height + TextExtraY/2 + ii * LineSize));
            ii += 1;
            graphics.DrawString(line, Arial, brush, new PointF(0, pos));
        }

        im.Save(fp);
    }

    public string GetPathToSave(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentNullException();
        }
        if (filename.Contains("_"))
        {
            var fp = filename.Split("_", 2);
            filename = fp[1];
        }
        while (true)
        {

            var joined = $"{ImageStorage}/{filename}";
            if (File.Exists(joined))
            {
                break;
            }

            return joined;
        }
        return "";
    }
}
