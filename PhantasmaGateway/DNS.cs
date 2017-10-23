using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PhantasmaGateway
{

    public class DNSUtils
    {
        public enum DNSKind
        {
            A,
            MX
        }

        public struct DNSEntry
        {
            public readonly int preference;
            public readonly string host;

            public DNSEntry(int preference, string host)
            {
                this.preference = preference;
                this.host = host;
            }
        }

        public static List<string> LookUp(string domain, DNSKind kind, string dnsServer = "8.8.8.8")
        {
            var entries = new List<DNSEntry>();

            string qtype;
            
            switch (kind)
            {
                case DNSKind.A: qtype = "1"; break;
                case DNSKind.MX: qtype = "15"; break;
                default: throw new Exception("Invalid DNS type");
            }

            UdpClient udpc = new UdpClient(dnsServer, 53);

            // SEND REQUEST--------------------
            List<byte> list = new List<byte>();
            list.AddRange(new byte[] { 88, 89, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0 });

            string[] tmp = domain.Split('.');
            foreach (string s in tmp)
            {
                list.Add(Convert.ToByte(s.Length));
                char[] chars = s.ToCharArray();
                foreach (char c in chars)
                    list.Add(Convert.ToByte(Convert.ToInt32(c)));
            }
            list.AddRange(new byte[] { 0, 0, Convert.ToByte(qtype), 0, 1 });

            byte[] req = new byte[list.Count];
            for (int i = 0; i < list.Count; i++) { req[i] = list[i]; }

            udpc.Send(req, req.Length);


            // RECEIVE RESPONSE--------------
            IPEndPoint ep = null;
            byte[] recv = udpc.Receive(ref ep);
            udpc.Close();

            int[] resp;

            resp = new int[recv.Length];
            for (int i = 0; i < resp.Length; i++)
                resp[i] = Convert.ToInt32(recv[i]);

            int status = resp[3];
            if (status != 128) throw new Exception(string.Format("{0}", status));
            int answers = resp[7];
            if (answers == 0) throw new Exception("No results");

            int pos = domain.Length + 18;
            if (qtype == "15") // MX record
            {
                while (answers > 0)
                {
                    int preference = resp[pos + 13];
                    pos += 14; //offset
                    string host = GetMXRecord(resp, pos, out pos);

                    entries.Add(new DNSEntry(preference, host));
                    answers--;
                }
            }
            else if (qtype == "1") // A record
            {
                while (answers > 0)
                {
                    pos += 11; //offset
                    string str = GetARecord(resp, ref pos);
                    entries.Add(new DNSEntry(0, str));
                    answers--;
                }
            }

            return entries.OrderBy(x => x.preference).Select( x => x.host).ToList();
        }

        //------------------------------------------------------
        private static string GetARecord(int[] resp, ref int start)
        {
            StringBuilder sb = new StringBuilder();

            int len = resp[start];
            for (int i = start; i < start + len; i++)
            {
                if (sb.Length > 0) sb.Append(".");
                sb.Append(resp[i + 1]);
            }
            start += len + 1;
            return sb.ToString();
        }

        private static string GetMXRecord(int[] resp, int start, out int pos)
        {
            StringBuilder sb = new StringBuilder();
            int len = resp[start];
            while (len > 0)
            {
                if (len != 192)
                {
                    if (sb.Length > 0) sb.Append(".");
                    for (int i = start; i < start + len; i++)
                        sb.Append(Convert.ToChar(resp[i + 1]));
                    start += len + 1;
                    len = resp[start];
                }
                if (len == 192)
                {
                    int newpos = resp[start + 1];
                    if (sb.Length > 0) sb.Append(".");
                    sb.Append(GetMXRecord(resp, newpos, out newpos));
                    start++;
                    break;
                }
            }
            pos = start + 1;
            return sb.ToString();
        }        
    }
}
