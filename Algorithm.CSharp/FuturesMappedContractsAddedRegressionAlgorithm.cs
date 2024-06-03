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

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class FuturesMappedContractsAddedRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        public override void Initialize()
        {
            SetStartDate(2018, 11, 30);
            SetEndDate(2019, 9, 1);

            var tickers = new[]
            {
                Futures.Grains.Soybeans,
                Futures.Grains.Wheat,
                Futures.Grains.SoybeanMeal,
                Futures.Grains.SoybeanOil,
                Futures.Grains.Corn,
                //Futures.Grains.Oats,
                Futures.Meats.LiveCattle,
                Futures.Meats.FeederCattle,
                Futures.Meats.LeanHogs,
                //Futures.Metals.Gold,
                //Futures.Metals.Silver,
                Futures.Metals.Platinum,
                Futures.Energies.BrentCrude,
                Futures.Energies.HeatingOil,
                Futures.Energies.NaturalGas,
                Futures.Energies.LowSulfurGasoil,
                Futures.Softs.Cotton2,
                Futures.Softs.OrangeJuice,
                Futures.Softs.Coffee,
                Futures.Softs.Cocoa,


                Futures.Grains.Oats,
                Futures.Metals.Gold,
                Futures.Metals.Silver,
            };

            foreach (var ticker in tickers)
            {
                AddFuture(ticker,
                    resolution: Resolution.Daily,
                    extendedMarketHours: true,
                    dataNormalizationMode: DataNormalizationMode.BackwardsRatio,
                    dataMappingMode: DataMappingMode.OpenInterest,
                    contractDepthOffset: 0);
            }

            SetBenchmark(x => 0);
        }

        public override void OnData(Slice data)
        {
            if (data.SymbolChangedEvents.Count > 0)
            {
                Log("***************************** OnData start *****************************************");
            }
            foreach (var symbol in data.SymbolChangedEvents)
            {
                var changedEvent = symbol.Value;
                var oldSymbol = changedEvent.OldSymbol;
                var newSymbol = changedEvent.NewSymbol;

                Log($"{Time} Rollover {oldSymbol} -> {newSymbol}");

                if (!Portfolio.ContainsKey(Symbol(oldSymbol)))
                {
                    Log($"{Time} - old symbol {oldSymbol} not in portfolio");
                    throw new System.Exception("AAAAAAAAAAAAAAAAAAAAAA");
                }
                if (!Securities.ContainsKey(Symbol(newSymbol)))
                {
                    Log($"{Time} - new symbol {newSymbol} not in self.securities");
                    throw new System.Exception("BBBBBBBBBBBBBBBBBBBBBB");
                }
            }
            if (data.SymbolChangedEvents.Count > 0)
            {
                Log("***************************** OnData end *****************************************\n");
            }

            //GC X0PFVD0VBJX9
            if (Securities.Keys.Any(x => x.ID.ToString() == "GC X1LXMG3FUG7X"))
            {

            }

            foreach (var key in Securities.Keys)
            {
                Log($"{key.ID}");
            }
        }

        public override void OnSecuritiesChanged(SecurityChanges changes)
        {
            changes.FilterInternalSecurities = false;

            if (changes.Count > 0)
            {
                Log("------------------------------ OnSecuritiesChanged start ------------------------------");
            }

            foreach (var added in changes.AddedSecurities)
            {
                Log($"{Time} Added: {added.Symbol}");
            }

            if (changes.AddedSecurities.Any(x => x.Symbol.ID.ToString() == "GC X1LXMG3FUG7X"))
            {

            }

            foreach (var removed in changes.RemovedSecurities)
            {
                Log($"{Time} Removed: {removed.Symbol}");
            }

            if (changes.Count > 0)
            {
                Log("------------------------------ OnSecuritiesChanged end ------------------------------\n");
            }
        }

















        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "1"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "271.453%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "101691.92"},
            {"Net Profit", "1.692%"},
            {"Sharpe Ratio", "8.854"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "67.609%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.005"},
            {"Beta", "0.996"},
            {"Annual Standard Deviation", "0.222"},
            {"Annual Variance", "0.049"},
            {"Information Ratio", "-14.565"},
            {"Tracking Error", "0.001"},
            {"Treynor Ratio", "1.97"},
            {"Total Fees", "$3.44"},
            {"Estimated Strategy Capacity", "$56000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "19.93%"},
            {"OrderListHash", "3da9fa60bf95b9ed148b95e02e0cfc9e"}
        };
    }
}
