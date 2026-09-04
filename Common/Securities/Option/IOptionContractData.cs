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

using QuantConnect.Data.Market;

namespace QuantConnect.Securities
{
    /// <summary>
    /// The option contract data the option filters work with,
    /// implemented by both option universe selection data and option chain contracts
    /// </summary>
    public interface IOptionContractData : IChainContractData
    {
        /// <summary>
        /// The greeks of the contract
        /// </summary>
        Greeks Greeks { get; }

        /// <summary>
        /// The implied volatility of the contract
        /// </summary>
        decimal ImpliedVolatility { get; }

        /// <summary>
        /// The open interest of the contract
        /// </summary>
        decimal OpenInterest { get; }
    }
}
