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

using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Securities.CurrencyConversion
{
    /// <summary>
    /// Provides an implementation of <see cref="ICurrencyConversion"/> with a fixed conversion rate
    /// </summary>
    public class DefaultCurrencyConversion : ICurrencyConversion
    {
        /// <summary>
        /// The currency this conversion converts from
        /// </summary>
        public string SourceCurrency { get; }

        /// <summary>
        /// The currency this conversion converts to
        /// </summary>
        public string DestinationCurrency { get; }

        /// <summary>
        /// The current conversion rate
        /// </summary>
        public decimal ConversionRate { get; set; }

        /// <summary>
        /// The securities which the conversion rate is based on
        /// </summary>
        public IEnumerable<Security> ConversionRateSecurities => Enumerable.Empty<Security>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCurrencyConversion"/> class.
        /// </summary>
        /// <param name="sourceCurrency">The currency this conversion converts from</param>
        /// <param name="destinationCurrency">The currency this conversion converts to</param>
        /// <param name="conversionRate">The conversion rate between the currencies</param>
        public DefaultCurrencyConversion(string sourceCurrency, string destinationCurrency, decimal conversionRate = 1m)
        {
            SourceCurrency = sourceCurrency;
            DestinationCurrency = destinationCurrency;
            ConversionRate = conversionRate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCurrencyConversion"/> class.
        /// The result instance is an identity conversion, i.e. the source and destination currencies are the same
        /// </summary>
        /// <param name="sourceCurrency">The currency this conversion converts from</param>
        /// <param name="destinationCurrency">The currency this conversion converts to</param>
        /// <param name="conversionRate">The conversion rate between the currencies</param>
        public DefaultCurrencyConversion(string accountCurrency)
            : this(accountCurrency, accountCurrency)
        {
        }

        /// <summary>
        /// Marks the conversion rate as potentially outdated, needing an update based on the latest data
        /// </summary>
        public void Update()
        {
        }
    }
}
