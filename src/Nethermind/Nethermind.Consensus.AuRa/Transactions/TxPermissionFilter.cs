﻿//  Copyright (c) 2018 Demerzel Solutions Limited
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
// 

using System;
using Nethermind.Consensus.AuRa.Contracts;
using Nethermind.Core;
using Nethermind.Core.Caching;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Logging;
using Nethermind.State;

namespace Nethermind.Consensus.AuRa.Transactions
{
    public class TxPermissionFilter : ITxPermissionFilter
    {
        private readonly TransactionPermissionContract _contract;
        private readonly ITxPermissionFilter.Cache _cache;
        private readonly IStateProvider _stateProvider;
        private readonly ILogger _logger;
        
        public TxPermissionFilter(TransactionPermissionContract contract, ITxPermissionFilter.Cache cache, IStateProvider stateProvider, ILogManager logManager)
        {
            _contract = contract ?? throw new ArgumentNullException(nameof(contract));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _logger = logManager?.GetClassLogger<TxPermissionFilter>() ?? throw new ArgumentNullException(nameof(logManager));
        }
        
        public bool IsAllowed(Transaction tx, BlockHeader blockHeader, long blockNumber)
        {
            if (blockNumber < _contract.Activation)
            {
                return true;
            }
            else
            {
                var txType = GetTxType(tx);
                var txPermissions = GetPermissions(tx, blockHeader);
                if (_logger.IsTrace) _logger.Trace($"Given transaction data: sender: {tx.SenderAddress} to: {tx.To} value: {tx.Value}, gas_price: {tx.GasPrice}. Permissions required: {txType}, got: {txPermissions}.");
                return (txPermissions & txType) == txType;
            }
        }

        private TransactionPermissionContract.TxPermissions GetPermissions(Transaction tx, BlockHeader blockHeader)
        {
            var key = (blockHeader.Hash, tx.SenderAddress);
            var txCachedPermissions = _cache.Permissions.Get(key);
            return txCachedPermissions ?? GetPermissionsFromContract(tx, blockHeader, key);
        }

        private TransactionPermissionContract.TxPermissions GetPermissionsFromContract(Transaction tx, BlockHeader blockHeader, in (Keccak Hash, Address SenderAddress) key)
        {
            TransactionPermissionContract.TxPermissions txPermissions = TransactionPermissionContract.TxPermissions.None;
            bool shouldCache = true;
            
            var versionedContract = GetVersionedContract(blockHeader);
            if (versionedContract == null)
            {
                if (_logger.IsError) _logger.Error("Unknown version of tx permissions contract is used.");
            }
            else
            {
                if (_logger.IsTrace) _logger.Trace($"Version of tx permission contract: {versionedContract.Version}.");
                
                try
                {
                    (txPermissions, shouldCache) = versionedContract.AllowedTxTypes(blockHeader, tx);
                }
                catch (AuRaException e)
                {
                    if (_logger.IsError) _logger.Error("Error calling tx permissions contract.", e);
                }
            }

            if (shouldCache)
            {
                _cache.Permissions.Set(key, txPermissions);
            }

            return txPermissions;
        }

        private TransactionPermissionContract.ITransactionPermissionVersionedContract GetVersionedContract(BlockHeader blockHeader)
        {
            TransactionPermissionContract.ITransactionPermissionVersionedContract versionedContract;
            if (_cache.VersionedContracts.TryGet(blockHeader.Hash, out var version))
            {
                versionedContract = _contract.GetVersionedContract(version);
            }
            else
            {
                versionedContract = _contract.GetVersionedContract(blockHeader);
                _cache.VersionedContracts.Set(blockHeader.Hash, versionedContract.Version);
            }

            return versionedContract;
        }

        private TransactionPermissionContract.TxPermissions GetTxType(Transaction tx)
        {
            return tx.IsContractCreation
                ? TransactionPermissionContract.TxPermissions.Create
                : _stateProvider.GetCodeHash(tx.To) != Keccak.OfAnEmptyString
                    ? TransactionPermissionContract.TxPermissions.Call
                    : TransactionPermissionContract.TxPermissions.Basic;
        }
    }
}