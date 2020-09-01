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

using System.Linq;
using FluentAssertions;
using Nethermind.Consensus.AuRa.Contracts;
using Nethermind.Consensus.AuRa.Transactions;
using Nethermind.Consensus.Transactions;
using Nethermind.Core;
using Nethermind.Core.Test.Builders;
using Nethermind.Int256;
using Nethermind.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.AuRa.Test.Transactions
{
    public class TxCertifierFilterTests
    {
        private ICertifierContract _certifierContract;
        private ITxFilter _notCertifiedFilter;
        private TxCertifierFilter _filter;

        [SetUp]
        public void SetUp()
        {
            _certifierContract = Substitute.For<ICertifierContract>();
            _notCertifiedFilter = Substitute.For<ITxFilter>();
            
            _notCertifiedFilter.IsAllowed(Arg.Any<Transaction>(), Arg.Any<BlockHeader>())
                .Returns(false);
            
            _certifierContract.Certified(Arg.Any<BlockHeader>(), 
                Arg.Is<Address>(a => TestItem.Addresses.Take(3).Contains(a)))
                .Returns(true);
            
            _filter = new TxCertifierFilter(_certifierContract, _notCertifiedFilter, LimboLogs.Instance);
        }
        
        [Test]
        public void should_allow_addresses_from_contract()
        {
            ShouldAllowAddress(TestItem.Addresses.First());
            ShouldAllowAddress(TestItem.Addresses.First());
            ShouldAllowAddress(TestItem.Addresses.Skip(1).First());
            ShouldAllowAddress(TestItem.Addresses.Skip(2).First());
        }
        
        [Test]
        public void should_not_allow_addresses_from_outside_contract()
        {
            ShouldAllowAddress(TestItem.AddressA, expected: false);
        }
        
        [TestCase(false)]
        [TestCase(true)]
        public void should_default_to_inner_contract_on_non_zero_transactions(bool expected)
        {
            _notCertifiedFilter.IsAllowed(Arg.Any<Transaction>(), Arg.Any<BlockHeader>())
                .Returns(expected);
            
            ShouldAllowAddress(TestItem.Addresses.First(), 1ul, expected);
        }
        
        private void ShouldAllowAddress(Address address, ulong gasPrice = 0ul, bool expected = true)
        {
            _filter.IsAllowed(
                Build.A.Transaction.WithGasPrice(gasPrice).WithSenderAddress(address).TestObject,
                Build.A.BlockHeader.TestObject).Should().Be(expected);
        }
    }
}