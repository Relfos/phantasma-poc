using System;
using System.Globalization;
using System.Text;
using System.Drawing;
using Lunar.Font;
using System.IO;

namespace Phantasma.Utils
{
    public static class CaptchaUtils
    {
        public static string captcha_key = "PHANTasma";

        private static AngelFont _captchaFont;

        public static void GenerateCaptcha(string fontPath, out string signature, out string pictureBase64)
        {
            if (_captchaFont == null)
            {
                _captchaFont = new AngelFont(fontPath);
            }

            var rnd = new System.Random();
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                char data = (char)(65 + rnd.Next() % 26);
                sb.Append(data);
            }

            var text = sb.ToString();

            int x1, y1, x2, y2;
            _captchaFont.GetRect(text, out x1, out y1, out x2, out y2);

            int pad = 8;
            var halfPad = (pad / 2);

            int margin = 10;
            var halfMargin = (margin / 2);

            var width = (x2 - x1) + 1 + pad + margin;
            var height = (y2 - y1) + 1 + pad + margin;

            var target = new Bitmap(width, height);
            
            int baseX = -x1 + halfPad + halfMargin;
            int x = baseX;
            int y = -y1 + halfPad + halfMargin;
            var g = Graphics.FromImage(target);
            var fnt = _captchaFont;

            foreach (var c in text)
            {
                if (c == '\n')
                {
                    y += fnt.lineAdvance;
                    x = baseX;
                }

                var info = fnt.GetCharInfo(c);
                if (info != null)
                {
                    var rndY = (rnd.Next() % pad) - halfPad;
                    fnt.DrawChar(g, x, y + rndY, info);

                    x += info.xadvance;
                }
            }

            //target.Save("captcha_test.png");

            var memoryStream = new MemoryStream();
            target.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

            var bytes = memoryStream.ToArray();
            var base64 = bytes.Base64Encode();

            signature = StringCipher.Encrypt(text, captcha_key);
            pictureBase64 = "data:image/png;base64, "+base64;
        }

        public static bool VerifyCatcha(string userInput, string signature)
        {
            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(signature))
            {
                return false;
            }

            try
            {
                var text = StringCipher.Decrypt(signature, captcha_key);
                return userInput.Equals(text);
            }
            catch
            {
                return false;
            }
        }
    }
}
