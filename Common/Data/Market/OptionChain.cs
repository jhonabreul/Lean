/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Python.Runtime;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Data.Market
{
    /// <summary>
    /// Represents an entire chain of option contracts for a single underlying security.
    /// This type is <see cref="IEnumerable{OptionContract}"/>.
    /// The chain can be narrowed down with the same filters available for option universe selection
    /// (see <see cref="IOptionContractFilters{TSelf}"/> and <see cref="OptionFilterUniverse"/>), e.g. <c>chain.calls_only().expiration(0, 30).strikes(-2, 2)</c>.
    /// Each filter returns a new chain, leaving this one untouched.
    /// </summary>
    public class OptionChain : BaseChain<OptionContract, OptionContracts>, IOptionContractFilters<OptionChain>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class
        /// </summary>
        /// <param name="canonicalOptionSymbol">The symbol for this chain.</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public OptionChain(Symbol canonicalOptionSymbol, DateTime time, bool flatten = true)
            : base(canonicalOptionSymbol, time, MarketDataType.OptionChain, flatten)
        {
        }

        /// <summary>
        /// Initializes a new option chain for a list of contracts as <see cref="OptionUniverse"/> instances
        /// </summary>
        /// <param name="canonicalOptionSymbol">The canonical option symbol</param>
        /// <param name="time">The time of this chain</param>
        /// <param name="contracts">The list of contracts data</param>
        /// <param name="symbolProperties">The option symbol properties</param>
        /// <param name="flatten">Whether to flatten the data frame</param>
        public OptionChain(Symbol canonicalOptionSymbol, DateTime time, IEnumerable<OptionUniverse> contracts, SymbolProperties symbolProperties,
            bool flatten = true)
            : this(canonicalOptionSymbol, time, flatten)
        {
            var underlyingSet = false;
            foreach (var contractData in contracts)
            {
                // The base constructor pre-sets an empty underlying, so it is replaced by the first actual underlying data found
                if (!underlyingSet && contractData.Underlying != null)
                {
                    Underlying = contractData.Underlying;
                    underlyingSet = true;
                }
                if (contractData.Symbol.ID.Date.Date < time.Date) continue;
                Contracts[contractData.Symbol] = OptionContract.Create(contractData, symbolProperties);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class as a clone of the specified instance
        /// </summary>
        private OptionChain(OptionChain other)
            : base(other)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionChain"/> class as a copy of the specified chain
        /// containing only the given subset of its contracts
        /// </summary>
        private OptionChain(OptionChain other, IEnumerable<OptionContract> contracts)
            : base(other, contracts)
        {
        }

        /// <summary>
        /// Return a new instance clone of this object, used in fill forward
        /// </summary>
        /// <returns>A clone of the current object</returns>
        public override BaseData Clone()
        {
            return new OptionChain(this);
        }

        #region Filters

        /// <summary>
        /// Selects the contracts with strikes in the given range relative to the underlying price, in number of strikes.
        /// Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Strikes"/>
        /// </summary>
        /// <param name="minStrike">The minimum strike relative to the underlying price, for example, -1 would filter out contracts further than 1 strike below market price</param>
        /// <param name="maxStrike">The maximum strike relative to the underlying price, for example, +1 would filter out contracts further than 1 strike above market price</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Strikes(int minStrike, int maxStrike)
        {
            return Filter(universe => universe.Strikes(minStrike, maxStrike));
        }

        /// <summary>
        /// Selects the contracts expiring in the given range relative to the chain date.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(TimeSpan, TimeSpan)"/>
        /// </summary>
        /// <param name="minExpiry">The minimum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiry">The maximum time until expiry to include, for example, TimeSpan.FromDays(10)
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Expiration(TimeSpan minExpiry, TimeSpan maxExpiry)
        {
            return Filter(universe => universe.Expiration(minExpiry, maxExpiry));
        }

        /// <summary>
        /// Selects the contracts expiring in the given range of days relative to the chain date.
        /// Same as <see cref="ContractSecurityFilterUniverse{T, TData}.Expiration(int, int)"/>
        /// </summary>
        /// <param name="minExpiryDays">The minimum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in less than 10 days</param>
        /// <param name="maxExpiryDays">The maximum time, expressed in days, until expiry to include, for example, 10
        /// would exclude contracts expiring in more than 10 days</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Expiration(int minExpiryDays, int maxExpiryDays)
        {
            return Filter(universe => universe.Expiration(minExpiryDays, maxExpiryDays));
        }

        /// <summary>
        /// Selects the call contracts. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.CallsOnly"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain CallsOnly()
        {
            return Filter(universe => universe.CallsOnly());
        }

        /// <summary>
        /// Selects the put contracts. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.PutsOnly"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain PutsOnly()
        {
            return Filter(universe => universe.PutsOnly());
        }

        /// <summary>
        /// Selects the standard contracts, excluding weeklys. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.StandardsOnly"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain StandardsOnly()
        {
            return Filter(universe => universe.StandardsOnly());
        }

        /// <summary>
        /// Selects the non standard weekly contracts. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.WeeklysOnly"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain WeeklysOnly()
        {
            return Filter(universe => universe.WeeklysOnly());
        }

        /// <summary>
        /// Selects the contracts of the nearest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.FrontMonth"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain FrontMonth()
        {
            return Filter(universe => universe.FrontMonth());
        }

        /// <summary>
        /// Selects the contracts of all expirations but the nearest one. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.BackMonths"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain BackMonths()
        {
            return Filter(universe => universe.BackMonths());
        }

        /// <summary>
        /// Selects the contracts of the second nearest expiration. Same as <see cref="ContractSecurityFilterUniverse{T, TData}.BackMonth"/>
        /// </summary>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain BackMonth()
        {
            return Filter(universe => universe.BackMonth());
        }

        /// <summary>
        /// Selects the contracts with delta in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Delta"/>
        /// </summary>
        /// <param name="min">The minimum delta value</param>
        /// <param name="max">The maximum delta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Delta(decimal min, decimal max)
        {
            return Filter(universe => universe.Delta(min, max));
        }

        /// <summary>
        /// Selects the contracts with delta in the given range. Alias for <see cref="Delta"/>
        /// </summary>
        /// <param name="min">The minimum delta value</param>
        /// <param name="max">The maximum delta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain D(decimal min, decimal max)
        {
            return Delta(min, max);
        }

        /// <summary>
        /// Selects the contracts with gamma in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Gamma"/>
        /// </summary>
        /// <param name="min">The minimum gamma value</param>
        /// <param name="max">The maximum gamma value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Gamma(decimal min, decimal max)
        {
            return Filter(universe => universe.Gamma(min, max));
        }

        /// <summary>
        /// Selects the contracts with gamma in the given range. Alias for <see cref="Gamma"/>
        /// </summary>
        /// <param name="min">The minimum gamma value</param>
        /// <param name="max">The maximum gamma value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain G(decimal min, decimal max)
        {
            return Gamma(min, max);
        }

        /// <summary>
        /// Selects the contracts with theta in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Theta"/>
        /// </summary>
        /// <param name="min">The minimum theta value</param>
        /// <param name="max">The maximum theta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Theta(decimal min, decimal max)
        {
            return Filter(universe => universe.Theta(min, max));
        }

        /// <summary>
        /// Selects the contracts with theta in the given range. Alias for <see cref="Theta"/>
        /// </summary>
        /// <param name="min">The minimum theta value</param>
        /// <param name="max">The maximum theta value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain T(decimal min, decimal max)
        {
            return Theta(min, max);
        }

        /// <summary>
        /// Selects the contracts with vega in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Vega"/>
        /// </summary>
        /// <param name="min">The minimum vega value</param>
        /// <param name="max">The maximum vega value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Vega(decimal min, decimal max)
        {
            return Filter(universe => universe.Vega(min, max));
        }

        /// <summary>
        /// Selects the contracts with vega in the given range. Alias for <see cref="Vega"/>
        /// </summary>
        /// <param name="min">The minimum vega value</param>
        /// <param name="max">The maximum vega value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain V(decimal min, decimal max)
        {
            return Vega(min, max);
        }

        /// <summary>
        /// Selects the contracts with rho in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.Rho"/>
        /// </summary>
        /// <param name="min">The minimum rho value</param>
        /// <param name="max">The maximum rho value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Rho(decimal min, decimal max)
        {
            return Filter(universe => universe.Rho(min, max));
        }

        /// <summary>
        /// Selects the contracts with rho in the given range. Alias for <see cref="Rho"/>
        /// </summary>
        /// <param name="min">The minimum rho value</param>
        /// <param name="max">The maximum rho value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain R(decimal min, decimal max)
        {
            return Rho(min, max);
        }

        /// <summary>
        /// Selects the contracts with implied volatility in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.ImpliedVolatility"/>
        /// </summary>
        /// <param name="min">The minimum implied volatility value</param>
        /// <param name="max">The maximum implied volatility value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain ImpliedVolatility(decimal min, decimal max)
        {
            return Filter(universe => universe.ImpliedVolatility(min, max));
        }

        /// <summary>
        /// Selects the contracts with implied volatility in the given range. Alias for <see cref="ImpliedVolatility"/>
        /// </summary>
        /// <param name="min">The minimum implied volatility value</param>
        /// <param name="max">The maximum implied volatility value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain IV(decimal min, decimal max)
        {
            return ImpliedVolatility(min, max);
        }

        /// <summary>
        /// Selects the contracts with open interest in the given range. Same as <see cref="BaseOptionFilterUniverse{TUniverse, TData}.OpenInterest"/>
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain OpenInterest(long min, long max)
        {
            return Filter(universe => universe.OpenInterest(min, max));
        }

        /// <summary>
        /// Selects the contracts with open interest in the given range. Alias for <see cref="OpenInterest"/>
        /// </summary>
        /// <param name="min">The minimum open interest value</param>
        /// <param name="max">The maximum open interest value</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain OI(long min, long max)
        {
            return OpenInterest(min, max);
        }

        /// <summary>
        /// Selects the contracts matching the given predicate, e.g. <c>chain.where(lambda contract: contract.open_interest > 100)</c>.
        /// From C# use Linq's Where, which keeps this chain's type untouched
        /// </summary>
        /// <param name="predicate">Function determining which contracts are kept</param>
        /// <returns>A new chain with the filter applied</returns>
        public OptionChain Where(PyObject predicate)
        {
            return new OptionChain(this, Contracts.Values.Where(predicate.SafeAs<Func<OptionContract, bool>>()));
        }

        /// <summary>
        /// Applies the given universe filter to the contracts of this chain and returns the result as a new chain
        /// </summary>
        private OptionChain Filter(Func<OptionChainFilterUniverse, OptionChainFilterUniverse> filter)
        {
            // the type filters (standards/weeklys) are only applied on demand, like the universe selection does after the user filter
            return new OptionChain(this, filter(new OptionChainFilterUniverse(this)).ApplyTypesFilter());
        }

        #endregion
    }
}
