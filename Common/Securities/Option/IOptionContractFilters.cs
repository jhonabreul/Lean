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

namespace QuantConnect.Securities
{
    /// <summary>
    /// The option contract filters shared by the option universe selection (<see cref="OptionFilterUniverse"/>)
    /// and the option chain (<see cref="Data.Market.OptionChain"/>), so both offer the same filters with the same semantics.
    /// Every filter added here must be implemented by both; <c>OptionChainTests.ChainExposesEveryUniverseFilter</c> verifies it
    /// </summary>
    /// <typeparam name="TSelf">The implementing type, returned by every filter for chaining</typeparam>
    public interface IOptionContractFilters<TSelf>
    {
        /// <summary>
        /// Selects the contracts with strikes in the given range relative to the underlying price, in number of strikes
        /// </summary>
        TSelf Strikes(int minStrike, int maxStrike);

        /// <summary>
        /// Selects the contracts expiring in the given range relative to the current date
        /// </summary>
        TSelf Expiration(TimeSpan minExpiry, TimeSpan maxExpiry);

        /// <summary>
        /// Selects the contracts expiring in the given range of days relative to the current date
        /// </summary>
        TSelf Expiration(int minExpiryDays, int maxExpiryDays);

        /// <summary>
        /// Selects the call contracts
        /// </summary>
        TSelf CallsOnly();

        /// <summary>
        /// Selects the put contracts
        /// </summary>
        TSelf PutsOnly();

        /// <summary>
        /// Selects the standard contracts, excluding weeklys
        /// </summary>
        TSelf StandardsOnly();

        /// <summary>
        /// Selects the non standard weekly contracts
        /// </summary>
        TSelf WeeklysOnly();

        /// <summary>
        /// Selects the contracts of the nearest expiration
        /// </summary>
        TSelf FrontMonth();

        /// <summary>
        /// Selects the contracts of all expirations but the nearest one
        /// </summary>
        TSelf BackMonths();

        /// <summary>
        /// Selects the contracts of the second nearest expiration
        /// </summary>
        TSelf BackMonth();

        /// <summary>
        /// Selects the contracts with delta in the given range
        /// </summary>
        TSelf Delta(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with delta in the given range. Alias for <see cref="Delta"/>
        /// </summary>
        TSelf D(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with gamma in the given range
        /// </summary>
        TSelf Gamma(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with gamma in the given range. Alias for <see cref="Gamma"/>
        /// </summary>
        TSelf G(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with theta in the given range
        /// </summary>
        TSelf Theta(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with theta in the given range. Alias for <see cref="Theta"/>
        /// </summary>
        TSelf T(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with vega in the given range
        /// </summary>
        TSelf Vega(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with vega in the given range. Alias for <see cref="Vega"/>
        /// </summary>
        TSelf V(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with rho in the given range
        /// </summary>
        TSelf Rho(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with rho in the given range. Alias for <see cref="Rho"/>
        /// </summary>
        TSelf R(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with implied volatility in the given range
        /// </summary>
        TSelf ImpliedVolatility(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with implied volatility in the given range. Alias for <see cref="ImpliedVolatility"/>
        /// </summary>
        TSelf IV(decimal min, decimal max);

        /// <summary>
        /// Selects the contracts with open interest in the given range
        /// </summary>
        TSelf OpenInterest(long min, long max);

        /// <summary>
        /// Selects the contracts with open interest in the given range. Alias for <see cref="OpenInterest"/>
        /// </summary>
        TSelf OI(long min, long max);
    }
}
