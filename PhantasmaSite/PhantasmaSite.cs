using Phantasma.Utils;
using LunarParser;
using SynkServer.Core;
using SynkServer.Entity;
using SynkServer.HTTP;
using SynkServer.Oauth;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using Neo.Cryptography;
using System.Security.Cryptography;
using LunarParser.XML;
using System.Text;
using LunarParser.JSON;
using NeoLux;
using PhantasmaMail;
using PhantasmaMail.Messages;
using System.Collections.Concurrent;
using SynkServer.Templates;

namespace Phantasma
{
    public class WhitelistUser
    {
        public string name;
        public string email;
        public string wallet;
        public string country;
    }

    public struct MailEntry
    {
        public string from;
        public string subject;
        public string body;
        public string date;
    }

    public static class WalletHelper
    {
        private static ThreadLocal<SHA256> _sha256 = new ThreadLocal<SHA256>(() => SHA256.Create());

        private static byte[] Sha256(this byte[] value, int offset, int count)
        {
            return _sha256.Value.ComputeHash(value, offset, count);
        }

        private static byte[] Sha256(this IEnumerable<byte> value)
        {
            return _sha256.Value.ComputeHash(value.ToArray());
        }

        private static byte[] Base58CheckDecode(string input)
        {
            byte[] buffer = Base58.Decode(input);
            if (buffer.Length < 4) throw new FormatException();
            byte[] checksum = buffer.Sha256(0, buffer.Length - 4).Sha256();
            if (!buffer.Skip(buffer.Length - 4).SequenceEqual(checksum.Take(4)))
                throw new FormatException();
            return buffer.Take(buffer.Length - 4).ToArray();
        }

        public static bool IsValidWallet(string address)
        {
            try
            {
                var buffer = Base58CheckDecode(address);
                return buffer != null && buffer.Length>0;
            }
            catch
            {
                return false;
            }
                
        }
    }

    public class PhantasmaSite
    {
        public static readonly string rootPath = "views/";
        public static readonly string whitelistFileName = "whitelist.xml";

        public static List<WhitelistUser> whitelist = new List<WhitelistUser>();
        public static Dictionary<string, WhitelistUser> whitelistEmailMap = new Dictionary<string, WhitelistUser>();
        public static Dictionary<string, WhitelistUser> whitelistWalletMap = new Dictionary<string, WhitelistUser>();

        public static void AddToWhitelist(WhitelistUser user)
        {
            if (user == null)
            {
                return;
            }

            lock (whitelist)
            {
                whitelistEmailMap[user.email] = user;
                whitelistWalletMap[user.wallet] = user;
                whitelist.Add(user);

                var root = DataNode.CreateArray("users");
                foreach (var entry in whitelist)
                {
                    var node = entry.ToDataSource();
                    root.AddNode(node);
                }

                try
                {
                    var content = XMLWriter.WriteToString(root);
                    File.WriteAllText(rootPath + whitelistFileName, content);
                }
                catch
                {

                }
            }
        }

        private static void Main(string[] args)
        {
            var log = new SynkServer.Core.Logger();

            var settings = ServerSettings.Parse(args);

            var server = new HTTPServer(log, settings);
            var site = new Site(server, "public");

            var keys = new Dictionary<string, KeyPair>();
            var lines = File.ReadAllLines(rootPath + "keys.txt");
            log.Info("Loadking keys...");
            foreach (var line in lines)
            {
                var temp = line.Split(',');
                var mail = temp[0];
                var key = temp[1];
                keys[mail] = new KeyPair(key.HexToBytes());
            }
            log.Info($"Loaded {keys.Count} keys!");

            log.Info("Initializing mailboxes...");

            var custom_mailboxes = new ConcurrentDictionary<string, Mailbox>();
            var default_mailboxes = new ConcurrentDictionary<string, Mailbox>();

            foreach (var entry in keys)
            {
                var mailbox = new Mailbox(entry.Value);
                default_mailboxes[entry.Key] = mailbox;

                if (string.IsNullOrEmpty(mailbox.name))
                {
                    log.Info("Registering mail: " + entry.Key);
                    mailbox.RegisterName(entry.Key);
                }
            }

            if (File.Exists(rootPath + whitelistFileName))
            {
                var xml = File.ReadAllText(rootPath + whitelistFileName);
                var root = XMLReader.ReadFromString(xml);

                try
                {
                    root = root["users"];

                    foreach (var node in root.Children)
                    {
                        if (node.Name.Equals("whitelistuser"))
                        {
                            var user = node.ToObject<WhitelistUser>();
                            if (user != null)
                            {
                                whitelist.Add(user);
                                whitelistEmailMap[user.email] = user;
                                whitelistWalletMap[user.wallet] = user;
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Error loading whitelist!");
                }
            }

            Console.WriteLine("Initializing server...");

            var cache = new FileCache(log, rootPath);

            Console.CancelKeyPress += delegate {
                Console.WriteLine("Closing service.");
                server.Stop();
                Environment.Exit(0);
            };

            var templateEngine = new TemplateEngine("views");

            site.Get("/", (request) =>
            {
                return HTTPResponse.FromString(File.ReadAllText(rootPath + "home.html"));
            });

            site.Get("/terms", (request) =>
            {
                return File.ReadAllBytes(rootPath + "terms.html");
            });

            site.Get("/demo", (request) =>
            {
                var currentMailbox = request.session.Get<Mailbox>("current", default_mailboxes.Values.FirstOrDefault());
                var context = new Dictionary<string, object>();

                var mailboxList = default_mailboxes.Values.ToList();

                var customMailbox = request.session.Get<Mailbox>("custom");
                if (customMailbox != null)
                {
                    mailboxList.Add(customMailbox);
                }

                context["mailboxes"] = mailboxList;

                context["currentMailbox"] = currentMailbox.name; 
                context["currentAddress"] = currentMailbox.address;

                var mails = new List<MailEntry>();

                lock (currentMailbox)
                {
                    foreach (Mail entry in currentMailbox.messages)
                    {
                        var mail = new MailEntry()
                        {
                            from = entry.fromAddress.Split('@')[0],
                            subject = entry.subject,
                            body = entry.body,
                            date = "12:10 AM"
                        };

                        mails.Insert(0, mail);
                    }
                }

                context["mails"] = mails.ToArray();
                context["empty"] = mails.Count == 0;

                var flash = request.session.Get<string>("flash");
                if (flash != null)
                {
                    context["flash"] = flash;
                    request.session.Remove("flash");
                }

                return templateEngine.Render(site, context, new string[] { "demo" });
            });

            site.Get("/demo/inbox/{id}", (request) =>
            {
                var id = request.args["id"];
                if (default_mailboxes.ContainsKey(id))
                {
                    var mailbox = default_mailboxes[id];
                    request.session.Set("current", mailbox);
                }
                else
                if (custom_mailboxes.ContainsKey(id))
                {
                    var mailbox = custom_mailboxes[id];
                    request.session.Set("current", mailbox);
                }
                return HTTPResponse.Redirect("/demo");
            });

            site.Post("/demo/custom", (request) =>
            {
                var emailStr = request.args["email"];

                var privateStr = request.args["private"];
                var privateKey = privateStr.HexToBytes();

                if (privateKey.Length == 32)
                {
                    var customKeys = new KeyPair(privateKey);
                    var mailbox = new Mailbox(customKeys);

                    if (string.IsNullOrEmpty(mailbox.name))
                    {
                        mailbox.RegisterName(emailStr);
                    }
                    else
                    if (mailbox.name != emailStr)
                    {
                        request.session.Set("flash", "Wrong mail for this address");
                        return HTTPResponse.Redirect("/demo");
                    }

                    request.session.Set("current", mailbox);
                    request.session.Set("custom", mailbox);

                    if (!custom_mailboxes.ContainsKey(emailStr))
                    {
                        custom_mailboxes[emailStr] = mailbox;
                        lock (mailbox)
                        {
                            mailbox.SyncMessages();
                        }
                    }
                        
                }

                return HTTPResponse.Redirect("/demo");
            });

            site.Post("/demo/send", (request) =>
            {
                var to = request.args["to"];
                var subject = request.args["subject"];
                var body = request.args["body"];

                var script = NeoAPI.GenerateScript(Protocol.scriptHash, "getAddressFromMailbox", new object[] { to });
                var invoke = NeoAPI.TestInvokeScript(Protocol.net, script);

                var temp = (byte[])invoke.result;
                if (temp != null && temp.Length > 0)
                {
                    var currentMailbox = request.session.Get<Mailbox>("current");

                    if (currentMailbox == null || string.IsNullOrEmpty(currentMailbox.name))
                    {
                        request.session.Set("flash", "Invalid mailbox selected");
                    }
                    else
                    {
                        var msg = Mail.Create(currentMailbox, to, subject, body);

                        try
                        {
                            if (currentMailbox.SendMessage(msg))
                            {
                                request.session.Set("flash", "Your message was sent to " + to);
                            }

                        }

                        catch (Exception e)
                        {
                            request.session.Set("flash", e.Message);
                        }

                    }
                }
                else {
                    request.session.Set("flash", to+" is not a valid Phantasma mailbox address");
                }

                return HTTPResponse.Redirect("/demo");
            });


            site.Post("/signup", (request) =>
            {
                var fullName = request.GetVariable("whitelist_name");
                var email = request.GetVariable("whitelist_email");
                var wallet = request.GetVariable("whitelist_wallet");
                var country = request.GetVariable("whitelist_country");

                var captcha = request.GetVariable("whitelist_captcha");
                var signature = request.GetVariable("whitelist_signature");

                string error = null;

                if (string.IsNullOrEmpty(fullName) || fullName.Length <= 5)
                {
                    error = "Full name is invalid";
                }
                else
                if (string.IsNullOrEmpty(email) || !email.Contains("@") || !email.Contains("."))
                {
                    error = "Email is invalid";
                }
                else
                if (string.IsNullOrEmpty(wallet) || !wallet.ToLower().StartsWith("a") || !WalletHelper.IsValidWallet(wallet))
                {
                    error = "Wallet does not seems to be a valid NEO address";
                }
                else
                if (string.IsNullOrEmpty(country))
                {
                    error = "Country is invalid";
                }
                else
                if (string.IsNullOrEmpty(captcha) || !CaptchaUtils.VerifyCatcha(captcha, signature))
                {
                    error = "Captcha is invalid";
                }
                else
                if (PhantasmaSite.whitelistEmailMap.ContainsKey(email))
                {
                    error = "Email already registered";
                }
                else
                if (PhantasmaSite.whitelistWalletMap.ContainsKey(wallet))
                {
                    error = "Wallet already registered";
                }

                var root = DataNode.CreateObject("signup");
                root.AddField("result", error != null ? "fail" : "success");

                if (error != null)
                {
                    root.AddField("error", error);
                }
                else
                {
                    var user = new WhitelistUser();
                    user.name = fullName;
                    user.email = email;
                    user.wallet = wallet;
                    user.country = country;

                    PhantasmaSite.AddToWhitelist(user);
                }

                var json = JSONWriter.WriteToString(root);
                return Encoding.UTF8.GetBytes(json);
            });

            site.Get("captcha/", (request) =>
            {
                var content = File.ReadAllText(rootPath + "captcha.html");

                string sign;
                string pic;
                CaptchaUtils.GenerateCaptcha(rootPath+ "captcha.fnt", out sign, out pic);

                content = content.Replace("$SIGNATURE", sign).Replace("$CAPTCHA", pic);

                return Encoding.UTF8.GetBytes(content);
            });

            #region EMAIL SYNC THREAD
            log.Info("Running email thread");

            var emailThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                do
                {
                    foreach (var mailbox in default_mailboxes.Values)
                    {
                        try
                        {
                            lock (mailbox)
                            {
                                mailbox.SyncMessages();
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    foreach (var mailbox in custom_mailboxes.Values)
                    {
                        try
                        {
                            lock (mailbox)
                            {
                                mailbox.SyncMessages();
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    var delay = (int)(TimeSpan.FromSeconds(5).TotalMilliseconds);
                    Thread.Sleep(delay);
                } while (true);
            });

            emailThread.Start();
            #endregion

            server.Run();
        }
    }
}


