/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using Nethermind.Core;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Store;

namespace Nethermind.Blockchain.TransactionPools
{
    public class TransactionPoolInfoProvider : ITransactionPoolInfoProvider
    {
        private readonly IStateProvider _stateProvider;

        public TransactionPoolInfoProvider(IStateProvider stateProvider)
        {
            _stateProvider = stateProvider;
        }

        public TransactionPoolInfo GetInfo(Transaction[] transactions)
        {
            var groupedTransactions = transactions.GroupBy(t => t.SenderAddress);
            var pendingTransactions = new Dictionary<Address, IDictionary<UInt256, Transaction[]>>();
            var queuedTransactions = new Dictionary<Address, IDictionary<UInt256, Transaction[]>>();
            foreach (var group in groupedTransactions)
            {
                var address = group.Key;
                var accountNonce = _stateProvider.GetNonce(address);
                var expectedNonce = accountNonce;
                var pending = new Dictionary<UInt256, Transaction[]>();
                var queued = new Dictionary<UInt256, Transaction[]>();
                var transactionsGroupedByNonce = group.OrderBy(t => t.Nonce).GroupBy(t => t.Nonce);

                foreach (var nonceGroup in transactionsGroupedByNonce)
                {
                    if (nonceGroup.Key < accountNonce)
                    {
                        queued.Add(nonceGroup.Key, nonceGroup.ToArray());
                        continue;
                    }

                    if (nonceGroup.Key == accountNonce ||
                        accountNonce != expectedNonce && nonceGroup.Key == expectedNonce)
                    {
                        pending.Add(nonceGroup.Key, nonceGroup.ToArray());
                        expectedNonce = nonceGroup.Key + 1;
                        continue;
                    }

                    queued.Add(nonceGroup.Key, nonceGroup.ToArray());
                }

                if (pending.Any())
                {
                    pendingTransactions[address] = pending;
                }

                if (queued.Any())
                {
                    queuedTransactions[address] = queued;
                }
            }

            return new TransactionPoolInfo(pendingTransactions, queuedTransactions);
        }
    }
}