using System;
using System.Text;
using System.Security.Cryptography;

namespace Phantasma.Utils
{
    public static class PasswordUtility
    {
        public static string Shuffle(this string str)
        {
            char[] array = str.ToCharArray();
            Random rng = new Random();
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
            return new string(array);
        }


        public static string MD5(this string input)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            return inputBytes.MD5();
        }

        public static string MD5(this byte[] inputBytes)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public static bool CheckPassword(string password, string user_hash)
        {
            var password_md5 = password.MD5();

            if (string.IsNullOrEmpty(user_hash))
            {
                return false;
            }

            var temp_hash = Crypt.crypt(password_md5.ToLower(), user_hash);
            return temp_hash.Equals(user_hash);
        }

        public static string GetPasswordHash(string password)
        {
            var key = password.MD5().ToLower();
            var s = "./ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'";
            s = s.Shuffle();
            s = s.Substring(s.Length - 4);
            var salt = "$1$" + s;
            return Crypt.crypt(key, salt);
        }
    }

}
