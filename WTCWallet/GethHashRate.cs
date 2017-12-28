using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Geth.RPC.Miner;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC;
using ApiMethods = Nethereum.Geth.RPC.ApiMethods;

namespace WTCWallet
{
    public class MinerHashrate : RpcRequestResponseHandler<String>
    {
        public MinerHashrate(IClient client)
            : base(client, "eth_hashrate")
        {
        }

        public Task<String> SendRequestAsync(object id = null)
        {
            return base.SendRequestAsync(id);
        }
    }

    public class CustomMinerApiService : RpcClientWrapper
    {
        public CustomMinerApiService(IClient client)
            : base(client)
        {
            this.MinerHashrate = new MinerHashrate(client);
        }

        public MinerHashrate MinerHashrate { get; private set; }

        public MinerStart Start { get; private set; }

        public MinerStop Stop { get; private set; }
    }
}