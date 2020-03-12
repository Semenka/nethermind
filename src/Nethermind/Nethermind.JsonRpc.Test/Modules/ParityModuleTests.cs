//  Copyright (c) 2018 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using Nethermind.Blockchain;
using Nethermind.Blockchain.Receipts;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Crypto;
using Nethermind.Db;
using Nethermind.Dirichlet.Numerics;
using Nethermind.JsonRpc.Modules.Parity;
using Nethermind.Logging;
using Nethermind.State;
using Nethermind.State.Repositories;
using Nethermind.Store;
using Nethermind.Store.Bloom;
using Nethermind.TxPool;
using Nethermind.TxPool.Storages;
using NUnit.Framework;

namespace Nethermind.JsonRpc.Test.Modules
{
    [TestFixture]
    public class ParityModuleTests
    {
        private IParityModule _parityModule;

        [SetUp]
        public void Initialize()
        {
            var logger = LimboLogs.Instance;
            var specProvider = MainNetSpecProvider.Instance;
            var ethereumEcdsa = new EthereumEcdsa(specProvider, logger);
            var txStorage = new InMemoryTxStorage();
            var txPool = new TxPool.TxPool(txStorage, Timestamper.Default, ethereumEcdsa, specProvider, new TxPoolConfig(),
                new StateProvider(new StateDb(), new MemDb(), LimboLogs.Instance),  LimboLogs.Instance);
            
            IDb blockDb = new MemDb();
            IDb headerDb = new MemDb();
            IDb blockInfoDb = new MemDb();
            IBlockTree blockTree = new BlockTree(blockDb, headerDb, blockInfoDb, new ChainLevelInfoRepository(blockInfoDb), specProvider, txPool, NullBloomStorage.Instance, LimboLogs.Instance);
            
            IReceiptStorage receiptStorage = new InMemoryReceiptStorage();
            _parityModule = new ParityModule(new EthereumEcdsa(specProvider,logger), txPool, blockTree, receiptStorage, logger);
            var blockNumber = 2;
            var pendingTransaction = Build.A.Transaction.Signed(ethereumEcdsa, TestItem.PrivateKeyD, blockNumber)
                .WithSenderAddress(Address.FromNumber((UInt256)blockNumber)).TestObject;
            pendingTransaction.Signature.V = 37;
            txPool.AddTransaction(pendingTransaction, blockNumber, TxHandlingOptions.None);
            
            blockNumber = 1;
            var transaction = Build.A.Transaction.Signed(ethereumEcdsa, TestItem.PrivateKeyD, blockNumber)
                .WithSenderAddress(Address.FromNumber((UInt256)blockNumber))
                .WithNonce(100).TestObject;
            transaction.Signature.V = 37;
            txPool.AddTransaction(transaction, blockNumber, TxHandlingOptions.None);

            
            Block genesis = Build.A.Block.Genesis
                .WithStateRoot(new Keccak("0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f"))
                .TestObject;
            
            blockTree.SuggestBlock(genesis);
            blockTree.UpdateMainChain(new[] {genesis}, true);

            Block previousBlock = genesis;
            Block block = Build.A.Block.WithNumber(blockNumber).WithParent(previousBlock)
                    .WithStateRoot(new Keccak("0x1ef7300d8961797263939a3d29bbba4ccf1702fabf02d8ad7a20b454edb6fd2f"))
                    .WithTransactions(transaction)
                    .TestObject;
                
            blockTree.SuggestBlock(block);
            blockTree.UpdateMainChain(new[] {block}, true);

            var logEntries = new[] {Build.A.LogEntry.TestObject};
            receiptStorage.Insert(block,new TxReceipt()
            {
                Bloom = new Bloom(logEntries),
                Index = 1,
                Recipient = TestItem.AddressA,
                Sender = TestItem.AddressB,
                BlockHash = TestItem.KeccakA,
                BlockNumber = 1,
                ContractAddress = TestItem.AddressC,
                GasUsed = 1000,
                TxHash = transaction.Hash,
                StatusCode = 0,
                GasUsedTotal = 2000,
                Logs = logEntries
            });
        }

        [Test]
        public void parity_pendingTransactions()
        {
            string serialized = RpcTest.TestSerializedRequest(_parityModule, "parity_pendingTransactions");
            var expectedResult = "{\"jsonrpc\":\"2.0\",\"result\":[{\"hash\":\"0xd4720d1b81c70ed4478553a213a83bd2bf6988291677f5d05c6aae0b287f947e\",\"nonce\":\"0x0\",\"blockHash\":null,\"blockNumber\":null,\"transactionIndex\":null,\"from\":\"0x0000000000000000000000000000000000000002\",\"to\":\"0x0000000000000000000000000000000000000000\",\"value\":\"0x1\",\"gasPrice\":\"0x1\",\"gas\":\"0x5208\",\"input\":\"0x\",\"raw\":\"0xf85f8001825208940000000000000000000000000000000000000000018025a0ef2effb79771cbe42fc7f9cc79440b2a334eedad6e528ea45c2040789def4803a0515bdfe298808be2e07879faaeacd0ad17f3b13305b9f971647bbd5d5b584642\",\"creates\":null,\"publicKey\":\"0x15a1cc027cfd2b970c8aa2b3b22dfad04d29171109f6502d5fb5bde18afe86dddd44b9f8d561577527f096860ee03f571cc7f481ea9a14cb48cc7c20c964373a\",\"chainId\":1,\"condition\":null,\"r\":\"0xef2effb79771cbe42fc7f9cc79440b2a334eedad6e528ea45c2040789def4803\",\"s\":\"0x515bdfe298808be2e07879faaeacd0ad17f3b13305b9f971647bbd5d5b584642\",\"v\":\"0x25\",\"standardV\":\"0x0\"}],\"id\":67}";
            Assert.AreEqual(expectedResult, serialized);
        }
        
        [Test]
        public void parity_getBlockReceipts()
        {
            string serialized = RpcTest.TestSerializedRequest(_parityModule, "parity_getBlockReceipts", "latest");
            var expectedResult = "{\"jsonrpc\":\"2.0\",\"result\":[{\"transactionHash\":\"0x026217c3c4eb1f0e9e899553759b6e909b965a789c6136d256674718617c8142\",\"transactionIndex\":\"0x0\",\"blockHash\":\"0xcbb80b69d74f3ea38aa1407ac2f7bab7df6010041a2c8f7e404a2e6696494b29\",\"blockNumber\":\"0x1\",\"cumulativeGasUsed\":\"0x7d0\",\"gasUsed\":\"0x7d0\",\"from\":\"0x0000000000000000000000000000000000000001\",\"to\":\"0x0000000000000000000000000000000000000000\",\"contractAddress\":null,\"logs\":[{\"removed\":false,\"logIndex\":\"0x0\",\"transactionIndex\":\"0x0\",\"transactionHash\":\"0x026217c3c4eb1f0e9e899553759b6e909b965a789c6136d256674718617c8142\",\"blockHash\":\"0xcbb80b69d74f3ea38aa1407ac2f7bab7df6010041a2c8f7e404a2e6696494b29\",\"blockNumber\":\"0x1\",\"address\":\"0x0000000000000000000000000000000000000000\",\"data\":\"0x\",\"topics\":[\"0x0000000000000000000000000000000000000000000000000000000000000000\"]}],\"logsBloom\":\"0x00000000000000000080000000000000000000000000000000000000000000000000000000000000000000000000000200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020000000000000000000800000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000020000000000000000000000000000000000000000000000000000000000000000000\",\"status\":\"0x1\"}],\"id\":67}";
            Assert.AreEqual(expectedResult, serialized);
        }
    }
}