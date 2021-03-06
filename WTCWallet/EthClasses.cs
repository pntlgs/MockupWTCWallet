﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using Nethereum.Geth;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.Web3;
using WTCWallet.Annotations;

namespace WTCWallet
{
    public class WTCAccountToken : AccountToken
    {
        public WTCAccountToken()
        {
            this.Symbol = "WTC";
        }
    }

    public class Token
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string ImgUrl { get; set; }
        public int NumberOfDecimalPlaces { get; set; }
    }




    public class WTCToken : Token
    {
        public WTCToken()
        {
            this.Name = "Walton";
            this.Symbol = "WTC";
            this.NumberOfDecimalPlaces = 18;
        }
    }

    public class AccountToken
    {
        public string Symbol { get; set; }
        public decimal Balance { get; set; }
    }

    public class AccountInfo
    {
        public AccountInfo()
        {
            this.WTC = new WTCAccountToken();
            this.AccountTokens = new List<AccountToken>();
        }
        public string Address { get; set; }
        public AccountToken WTC { get; set; }
        public List<AccountToken> AccountTokens { get; set; }

    }

    public interface IWTCWalletService
    {
        Task<AccountInfo> GetAccountsInfo(string address);

    }

    public interface IWalletConfigurationService
    {
        string ClientUrl { get; set; }

        IClient Client { get; set; }
    }

    public class WalletConfigurationService : IWalletConfigurationService
    {
        public WalletConfigurationService()
        {
            this.ClientUrl = "http://127.0.0.1:8546/";
            //this.ClientUrl = "http://127.0.0.1:8545/";
            this.Client = new RpcClient(new Uri(ClientUrl));
        }

        private IClient client;
        public IClient Client
        {
            get
            {
                return client;
            }

            set
            {
                client = value;
            }
        }

        public string ClientUrl { get; set; }


    }

    public class WTCWalletService : IWTCWalletService
    {
        private IWalletConfigurationService configuration;

        public WTCWalletService(IWalletConfigurationService configuration)
        {
            this.configuration = configuration;
        }


        public async Task<AccountInfo> GetAccountsInfo(string address)
        {
            //var web3 = GetWeb3();
     
                try
                {

             
                    var weiBalance = await AppVM.Geth.Eth.GetBalance.SendRequestAsync(address);
                    var balance = (decimal)weiBalance.Value / (decimal)Math.Pow(10, 18);
                    var accountInfo = new AccountInfo { Address = address, WTC = new WTCAccountToken() };
                    accountInfo.WTC.Balance = balance;

                    return accountInfo;


                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            return null;

            //   return accountsInfo;
        }

        public IEnumerable<BlockVM> GetMinerBlocks(string address, int page)
        {
            //http://waltonchain.net/minerpagination/0xc0e4d8d1dC02cAA500D00E28f7aA7c68DE836450/1
            WebClient client = new WebClient();

            var str = "<Root>" + client.DownloadString(
                          $"http://waltonchain.net/minerpagination/{address}/{page}") + "</Root>";

            XmlDocument document = new XmlDocument();
            document.LoadXml(str);

            var table = document.GetElementsByTagName("table").OfType<XmlElement>().FirstOrDefault();

            int rowCount = 0;

            foreach (XmlElement row in table.ChildNodes)
            {
                if (rowCount++ == 0)
                    continue;

                int i = 0;
                BlockVM vm = new BlockVM();
                foreach (XmlElement rowChildNode in row.ChildNodes)
                {
                    if (i == 0)
                    {
                        vm.BlockNumber = rowChildNode.InnerText.Trim();
                    }
                    else if (i == 1)
                    {
                        vm.Hash = rowChildNode.InnerText.Trim();
                    }
                    else if (i == 2)
                    {
                        vm.Reward = rowChildNode.InnerText.Trim();
                    }
                    else if (i == 3)
                    {
                        vm.Difficulty = rowChildNode.InnerText.Trim();
                    }
                    else if (i == 4)
                    {
                        vm.Nounce = rowChildNode.InnerText.Trim();
                    }
                    else if (i == 5)
                    {
                        vm.Size = rowChildNode.InnerText.Trim();
                    }

                    i++;
                }

                yield return vm;
            }
        }



        public IEnumerable<TransactionVM> GetLatestTransactions(string address, int page)
        {
            WebClient client = new WebClient();

            var str = "<Root>" + client.DownloadString(
                          $"http://waltonchain.net/transactionpagination/{address}/{page}") + "</Root>";

            XmlDocument document = new XmlDocument();
            document.LoadXml(str);

            var table = document.GetElementsByTagName("table").OfType<XmlElement>().FirstOrDefault();

            int rowCount = 0;

            foreach (XmlElement row in table.ChildNodes)
            {
                if (rowCount++ == 0)
                    continue;

                int i = 0;
                TransactionVM vm = new TransactionVM();
                foreach (XmlElement rowChildNode in row.ChildNodes)
                {
                    

                    if (i == 0)
                    {
                        vm.BlockNumber = rowChildNode.InnerText.Trim();
                    }
                    else if (i == 1)
                    {
                        vm.Hash = rowChildNode.InnerText.Trim();
                    }
                    else if (i == 2)
                    { 
                    }
                    else if (i == 3)
                    {
                        vm.Amount = rowChildNode.InnerText.Trim();
                    }

                    i++;
                }

                yield return vm;
            }
        }
    }


  
}