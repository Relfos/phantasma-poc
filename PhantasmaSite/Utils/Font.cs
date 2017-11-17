using System.IO;
using System.Collections.Generic;
using System.Drawing;
using LunarParser.XML;
using System;

namespace Lunar.Font
{
    public class FontChar
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public int xoffset;
        public int yoffset;
        public int xadvance;
        public int page;
        public Bitmap image;
    }

    public class AngelFont
    {
        public string name;

        public int height;
        public int baseY;

        public Dictionary<int, Bitmap> pages = new Dictionary<int, Bitmap>();
        public Dictionary<int, FontChar> chars = new Dictionary<int, FontChar>();

        public int lineAdvance { get; private set; }

        public AngelFont(string path)
        {
            string xml = File.ReadAllText(path);
            var root = XMLReader.ReadFromString(xml);

            path = Path.GetDirectoryName(path);

            if (!"font".Equals(root.Name))
            {
                root = root.GetNode("font");
            }

            var info = root.GetNode("info");
            //name = info.ReadString("face");
            name = Path.GetFileNameWithoutExtension(path);

            info = root.GetNode("common");
            this.height = info.GetInt32("lineHeight");
            this.baseY = info.GetInt32("base");

            int kerning = info.GetInt32("kerning");

            var pages = root.GetNode("pages");
            foreach (var child in pages.Children)
            {
                int id = child.GetInt32("id");
                string imageFilename = child.GetString("file");

                imageFilename = Path.Combine(path, imageFilename);

                var bitmap = new Bitmap(Image.FromFile(imageFilename));

                this.pages.Add(id, bitmap);
            }

            var chars = root.GetNode("chars");
            foreach (var child in chars.Children)
            {
                if (!child.Name.ToLower().Equals("char"))
                {
                    continue;
                }

                var c = new FontChar();
                int id = child.GetInt32("id");
                c.x = child.GetInt32("x");
                c.y = child.GetInt32("y");
                c.width = child.GetInt32("width");
                c.height = child.GetInt32("height");
                c.xoffset = child.GetInt32("xoffset");
                c.yoffset = child.GetInt32("yoffset");
                c.xadvance = child.GetInt32("xadvance") + (id!=32 ? kerning : 0);
                c.page = child.GetInt32("page");

                // An empty bitmap which will hold the cropped image
                c.image = new Bitmap(c.width, c.height);

                var g = Graphics.FromImage(c.image);

                var page = this.pages[c.page];
                var section = new Rectangle(c.x, c.y, c.width, c.height);

                // Draw the given area (section) of the source image
                // at location 0,0 on the empty bitmap (bmp)
                g.DrawImage(page, 0, 0, section, GraphicsUnit.Pixel);

                this.chars.Add(id, c);
            }

            lineAdvance = this.chars[(int)'H'].height;
        }

        public FontChar GetCharInfo(char c)
        {
            int ID = (int)c;
            if (chars.ContainsKey(ID))
            {
                return chars[ID];
            }

            return null;
        }

        public void GetRect(string text, out int x1, out int y1, out int x2, out int y2)
        {
            int x = 0;
            int y = 0;

            x1 = 9999;
            y1 = 9999;

            x2 = -9999;
            y2 = -9999;

            foreach (var c in text)
            {
                if (c == '\n')
                {
                    y += lineAdvance;
                    x = 0;
                }

                var info = GetCharInfo(c);
                if (info != null)
                {
                    var locX1 = x + info.xoffset;
                    var locY1 = y + info.yoffset;
                    var locX2 = locX1 + info.width;
                    var locY2 = locY1 + info.height;

                    x1 = Math.Min(x1, locX1);
                    y1 = Math.Min(y1, locY1);

                    x2 = Math.Max(x2, locX2);
                    y2 = Math.Max(y2, locY2);

                    x += info.xadvance;
                }
            }
        }

        public void DrawChar(Graphics g, int x, int y, FontChar ch)
        {
            if (ch == null || g == null)
            {
                return;
            }

            x += ch.xoffset;
            y += ch.yoffset;

            g.DrawImage(ch.image, new Point(x, y));

        }

        public void DrawText(Bitmap target, int x, int y, string text)
        {
            int baseX = x;
            var g = Graphics.FromImage(target);

            foreach (var c in text)
            {
                if (c == '\n')
                {
                    y += lineAdvance;
                    x = baseX;
                }

                var info = GetCharInfo(c);
                if (info != null)
                {
                    DrawChar(g, x, y, info);

                    x += info.xadvance;                    
                }
            }
        }
    }
}