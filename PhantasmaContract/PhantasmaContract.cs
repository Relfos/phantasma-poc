/*

Phantasma Smart Contract
========================

Author: Sérgio Flores

Phantasma mail protocol 

txid: 0xf1f418b3235214aba77bb9ecd72b820c3f74419835aa58f045e195d88aba996a

script hash: 0xde1a53be359e8be9f3d11627bcca40548a2d5bc1


pubkey: 029ada24a94e753729768b90edee9d24ec9027cb64cea406f8ab296fce264597f4

*/
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Text;

namespace Neo.SmartContract
{
    public class PhantasmaContract : Framework.SmartContract
    {
        //  params: 0710
        // return : 05
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                #region MAIL METHODS
                if (operation == "getMailboxFromAddress")
                {
                    if (args.Length != 1) return false;
                    byte[] address = (byte[])args[0];
                    return GetMailboxFromAddress(address);
                }
                if (operation == "getAddressFromMailbox")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetAddressFromMailbox(mailbox);
                }
                if (operation == "registerMailbox")
                {
                    if (args.Length != 2) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] name = (byte[])args[1];
                    return RegisterMailbox(owner, name);
                }
                if (operation == "sendMessage")
                {
                    if (args.Length != 3) return false;
                    byte[] owner = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    byte[] hash = (byte[])args[2];
                    return SendMessage(owner, to, hash);
                }
                if (operation == "getMailCount")
                {
                    if (args.Length != 1) return false;
                    byte[] mailbox = (byte[])args[0];
                    return GetMailCount(mailbox);
                }

                if (operation == "getMailContent")
                {
                    if (args.Length != 2) return false;
                    byte[] mailbox = (byte[])args[0];
                    BigInteger index = (BigInteger)args[1];
                    return GetMailContent(mailbox, index);
                }

                /*          if (operation == "notifySubscribers")
                          {
                              if (args.Length != 3) return false;
                              byte[] from = (byte[])args[0];
                              byte[] signature = (byte[])args[1];
                              byte[] hash = (byte[])args[2];
                              return NotifySubscribers(from, signature, hash);
                          }

                          if (operation == "getSubscriberCount")
                          {
                              if (args.Length != 1) return false;
                              byte[] mailbox = (byte[])args[0];
                              return GetSubscriberCount(mailbox);
                          }

                          if (operation == "subscribeTo")
                          {
                              if (args.Length != 3) return false;
                              byte[] from = (byte[])args[0];
                              byte[] signature = (byte[])args[1];
                              byte[] to = (byte[])args[2];
                              return SubscribeTo(from, signature, to);
                          }*/

                #endregion

                return false;
            }

            return true;
        }

        #region MAILBOX API
        private static readonly byte[] mailbox_prefix = { (byte)'M', (byte)'B', (byte)'O', (byte)'X' };
        private static readonly byte[] address_prefix = { (byte)'M', (byte)'A', (byte)'D', (byte)'R' };
        private static readonly byte[] mailsize_prefix = { (byte)'M', (byte)'S', (byte)'I', (byte)'Z' };
        private static readonly byte[] mailcontent_prefix = { (byte)'M', (byte)'C', (byte)'N', (byte)'T' };

        private static byte[] GetMailboxFromAddress(byte[] address)
        {
            var key = address_prefix.Concat(address);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        private static byte[] GetAddressFromMailbox(byte[] mailbox)
        {
            var key = mailbox_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        private static bool RegisterMailbox(byte[] owner, byte[] mailbox)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            //if (!VerifySignature(owner, signature)) return false;

            var key = mailbox_prefix.Concat(mailbox);
            byte[] value = Storage.Get(Storage.CurrentContext, key);

            // verify if name already in use
            if (value != null) return false;

            // save owner of name 
            Storage.Put(Storage.CurrentContext, key, owner);

            // save reverse mapping address => name
            key = address_prefix.Concat(owner);
            Storage.Put(Storage.CurrentContext, key, mailbox);

            // initialize subscriber list
  /*          key = groupsize_prefix.AsByteArray().Concat(owner);
            BigInteger groupSize = 0;
            Storage.Put(Storage.CurrentContext, key, groupSize);
    */        
            return true;
        }

        private static bool SendMessage(byte[] owner, byte[] to, byte[] hash)
        {
            if (!Runtime.CheckWitness(owner)) return false;

            return SendMessageVerified(to, hash);
        }

        private static bool SendMessageVerified(byte[] to, byte[] hash)
        {
            var key = mailbox_prefix.Concat(to);
            var value = Storage.Get(Storage.CurrentContext, key);

            // verify if name exists
            if (value == null) return false;

            // get mailbox current size
            key = mailsize_prefix.Concat(to);
            value = Storage.Get(Storage.CurrentContext, key);

            // increase size and save
            var mailcount = value.AsBigInteger() + 1;
            value = mailcount.AsByteArray();
            Storage.Put(Storage.CurrentContext, key, value);

            key = mailcontent_prefix.Concat(to);
            value = mailcount.AsByteArray();
            key = key.Concat(value);

            // save mail content / hash
            Storage.Put(Storage.CurrentContext, key, hash);

            return true;

        }

        private static BigInteger GetMailCount(byte[] mailbox)
        {
            // get mailbox current size
            var key = mailsize_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);

            return value.AsBigInteger();
        }

        private static byte[] GetMailContent(byte[] mailbox, BigInteger index)
        {
            if (index <= 0)
            {
                return null;
            }

            // get mailbox current size
            var key = mailsize_prefix.Concat(mailbox);
            var value = Storage.Get(Storage.CurrentContext, key);

            var size = value.AsBigInteger();
            if (index>size)
            {
                return null;
            }

            key = mailcontent_prefix.Concat(mailbox);
            value = index.AsByteArray();
            key = key.Concat(value);

            // save mail content / hash
            value = Storage.Get(Storage.CurrentContext, key);
            return value;
        }

        /*        private static readonly string groupsize_prefix = "GSIZ";
                private static readonly string groupmember_prefix = "GMBR";

                // this will send a message to every subscriber
                private static bool NotifySubscribers(byte[] from, byte[] hash)
                {
                    if (!Runtime.CheckWitness(from)) return false;

                    var key = mailbox_prefix.AsByteArray().Concat(from);
                    var value = Storage.Get(Storage.CurrentContext, key);

                    // verify if name exists
                    if (value == null) return false;

                    // get subscribers current count
                    key = groupsize_prefix.AsByteArray().Concat(from);
                    value = Storage.Get(Storage.CurrentContext, key);

                    var subscriberCount = value.AsBigInteger();

                    while (subscriberCount>0)
                    {
                        key = groupmember_prefix.AsByteArray().Concat(from);
                        key = key.Concat(subscriberCount.AsByteArray());

                        value = Storage.Get(Storage.CurrentContext, key);

                        SendMessageVerified(from, value, hash);

                        subscriberCount--;
                    }

                    return true;
                }

                private static BigInteger GetSubscriberCount(byte[] mailbox)
                {
                    // get subscriberr current count
                    var key = groupsize_prefix.AsByteArray().Concat(mailbox);
                    var value = Storage.Get(Storage.CurrentContext, key);

                    return value.AsBigInteger();
                }

                private static bool SubscribeTo(byte[] from, byte[] signature, byte[] to)
                {
                    if (!VerifySignature(from, signature)) return false;

                    // get mailbox current subscribers
                    var key = groupsize_prefix.AsByteArray().Concat(to);
                    var value = Storage.Get(Storage.CurrentContext, key);

                    var subCount = value.AsBigInteger();
                    subCount++;

                    value = subCount.AsByteArray();
                    Storage.Put(Storage.CurrentContext, key, value);

                    key = groupmember_prefix.AsByteArray().Concat(to);
                    key = key.Concat(value);

                    // save subscriber
                    Storage.Put(Storage.CurrentContext, key, from);
                    return true;
                }*/
        #endregion
    }
}
