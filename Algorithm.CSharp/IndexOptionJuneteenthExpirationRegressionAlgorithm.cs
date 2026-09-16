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

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Asserts the SPX contracts dated Friday June 20th 2025, the day after the Juneteenth holiday, are zero DTE on that Friday only:
    /// on Wednesday the 18th they must be two days out, both in the option chain and in the zero DTE universe selection
    /// </summary>
    public class IndexOptionJuneteenthExpirationRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private static readonly DateTime Expiry = new(2025, 6, 20);
        private static readonly DateTime Juneteenth = new(2025, 6, 19);

        private Symbol _spxOption;
        private int _chainChecks;
        private int _zeroDteSelections;

        public override void Initialize()
        {
            SetStartDate(2025, 6, 18);
            SetEndDate(2025, 6, 20);
            SetCash(100000);

            var spx = AddIndex("SPX");
            var option = AddIndexOption(spx.Symbol);
            option.SetFilter(u => u.Expiration(0, 0));
            _spxOption = option.Symbol;
            SetBenchmark(spx.Symbol);

            Schedule.On(DateRules.EveryDay(_spxOption), TimeRules.At(10, 0), CheckChain);
        }

        private void CheckChain()
        {
            var chain = OptionChain(_spxOption);
            if (chain.Any(x => x.Expiry == Juneteenth))
            {
                throw new RegressionTestException($"{Time}: the chain has contracts expiring on Juneteenth, a market holiday");
            }

            var contracts = chain.Where(x => x.Expiry == Expiry).Select(x => x.Symbol).ToHashSet();
            if (contracts.Count == 0)
            {
                throw new RegressionTestException($"{Time}: the chain has no contracts expiring on {Expiry:yyyy-MM-dd}");
            }

            var zeroDte = chain.ZeroDte().Select(x => x.Symbol).ToHashSet();
            var moreThanZeroDte = chain.Expiration(1, 30).Where(x => x.Expiry == Expiry).Select(x => x.Symbol).ToHashSet();
            var expectedDaysToExpiry = (Expiry - Time.Date).Days;
            var expectedZeroDte = expectedDaysToExpiry == 0 ? contracts : new HashSet<Symbol>();
            var expectedMoreThanZeroDte = expectedDaysToExpiry == 0 ? new HashSet<Symbol>() : contracts;

            if (!zeroDte.SetEquals(expectedZeroDte))
            {
                throw new RegressionTestException($"{Time}: expected {expectedZeroDte.Count} zero DTE contracts but got {zeroDte.Count}");
            }
            if (!moreThanZeroDte.SetEquals(expectedMoreThanZeroDte))
            {
                throw new RegressionTestException($"{Time}: expected {expectedMoreThanZeroDte.Count} contracts more than zero DTE but got {moreThanZeroDte.Count}");
            }
            var offContract = chain.Where(x => contracts.Contains(x.Symbol)).FirstOrDefault(x => x.DaysToExpiry != expectedDaysToExpiry);
            if (offContract != null)
            {
                throw new RegressionTestException($"{Time}: expected {expectedDaysToExpiry} days to expiry for the {Expiry:yyyy-MM-dd} contracts " +
                    $"but {offContract.Symbol.Value} has {offContract.DaysToExpiry}, its time is {offContract.Time}");
            }

            _chainChecks++;
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            var added = changes.AddedSecurities.Where(x => x.Symbol.SecurityType == SecurityType.IndexOption && !x.Symbol.IsCanonical()).ToList();
            if (added.Count == 0)
            {
                return;
            }

            // Only the June contracts can be zero DTE in this window, and only for the Friday session
            if (added.Any(x => x.Symbol.ID.Date != Expiry))
            {
                throw new RegressionTestException($"{Time}: the zero DTE selection added contracts not expiring on {Expiry:yyyy-MM-dd}");
            }
            // Selection runs at the exchange midnight for the next session, so on a holiday it runs for the day after
            var hours = Securities[_spxOption].Exchange.Hours;
            var exchangeDate = UtcTime.ConvertFromUtc(hours.TimeZone).Date;
            var session = hours.IsDateOpen(exchangeDate) ? exchangeDate : hours.GetNextTradingDay(exchangeDate);
            if (session != Expiry)
            {
                throw new RegressionTestException($"{Time}: the zero DTE selection added the {Expiry:yyyy-MM-dd} contracts for the {session:yyyy-MM-dd} session");
            }

            _zeroDteSelections++;
        }

        public override void OnEndOfAlgorithm()
        {
            if (_chainChecks != 2)
            {
                throw new RegressionTestException($"Expected the chain to be checked on the 18th and the 20th but it was checked {_chainChecks} times");
            }
            if (_zeroDteSelections != 1)
            {
                throw new RegressionTestException($"Expected a single zero DTE selection but got {_zeroDteSelections}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 0;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"Drawdown Recovery", "0"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
