using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Visualization
{
    public static class RadialProgressBar
    {
        public static Bitmap drawRadialProgressBar(int size, int percent, string title)
        {
            int penSize = 6;
            Bitmap p1 = new Bitmap(size + 500, size);
            Graphics g = Graphics.FromImage(p1);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Brush b1 = new SolidBrush(Color.Black);
            Pen blackPen = new Pen(Color.DarkBlue, penSize);
            Pen grayPen = new Pen(Color.LightBlue, penSize);

            g.DrawArc(grayPen, penSize / 2, penSize / 2, size - penSize, size - penSize, -90, 360);
            g.DrawArc(blackPen, penSize / 2, penSize / 2, size - penSize, size - penSize, -90, (int)(percent * 3.6));

            // Set format of string.
            StringFormat drawFormat = new StringFormat();
            drawFormat.FormatFlags = StringFormatFlags.NoWrap;

            if (percent == 100)
            {
                penSize -= 6;
                g.FillEllipse(new SolidBrush(Color.FromArgb(80, 0, 255, 0)), penSize / 2, penSize / 2, size - penSize, size - penSize);

                g.DrawString(title , new Font("Arial", 12), b1, size+10, size / 2-5, drawFormat);
            }
            else
                g.DrawString(title, new Font("Arial", 12), b1, size+10, size/2-5, drawFormat);
           
            g.DrawString(percent + "%", new Font("Arial", 14), b1, 10, size / 2 - 8, drawFormat);

            return p1;
        }
    }
}
