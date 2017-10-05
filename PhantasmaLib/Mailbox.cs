using System;

namespace PhantasmaMail
{
    public class Mailbox
    {
        public string address { get; private set; }

        public Mailbox(byte[] privateKey)
        {
            this.address = "demo@phantasma.io";
        }
    }
}
