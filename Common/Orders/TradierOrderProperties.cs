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
 *
*/

namespace QuantConnect.Orders
{
    /// <summary>
    /// Provides an implementation of the <see cref="OrderProperties"/> specific to Tradier order.
    /// </summary>
    public class TradierOrderProperties : OrderProperties
    {
        /// <summary>
        /// If set to true, allows orders to also trigger and fill outside of regular trading hours.
        /// If on extended hours, the order will be valid only during the current extended session.
        /// </summary>
        public bool OutsideRegularTradingHours { get; set; }
    }
}
