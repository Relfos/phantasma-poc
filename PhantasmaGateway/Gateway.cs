using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhantasmaGateway
{
    class Gateway
    {
        static void Main(string[] args)
        {
            var ips = DNSUtils.LookUp("gmail.com", DNSUtils.DNSKind.MX);
            foreach (var ip in ips)
            {
                Console.WriteLine(ip);
            }
        }
    }
}
