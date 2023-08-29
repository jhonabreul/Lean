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

using QuantConnect.Data;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using QuantConnect.Orders.Fills;
using System;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities.Volatility;
using QuantConnect.Securities.Option;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// </summary>
    public class OptionModelsConsistencyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected Symbol _optionSymbol;

        private bool _modelsChecked;

        public override void Initialize()
        {
            var option = InitializeAlgorithm();
            _optionSymbol = option.Symbol;
            SetModels(option);
        }

        public override void OnData(Slice slice)
        {
            if (slice.OptionChains.TryGetValue(_optionSymbol, out var chain))
            {
                foreach (var contract in chain)
                {
                    var optionContract = Securities[contract.Symbol];

                    // Check that contracts fill model is our custom fill model
                    if (optionContract.FillModel.GetType() != typeof(CustomFillModel))
                    {
                        throw new Exception($@"Contract {optionContract.Symbol} has fill model {optionContract.FillModel.GetType()
                            } instead of CustomFillModel");
                    }

                    // Check that contracts fee model is our custom fee model
                    if (optionContract.FeeModel.GetType() != typeof(CustomFeeModel))
                    {
                        throw new Exception($@"Contract {optionContract.Symbol} has fee model {optionContract.FeeModel.GetType()
                            } instead of CustomFeeModel");
                    }

                    // Check that contracts buying power model is our custom buying power model
                    if (optionContract.BuyingPowerModel.GetType() != typeof(CustomBuyingPowerModel))
                    {
                        throw new Exception($@"Contract {optionContract.Symbol} has buying power model {optionContract.BuyingPowerModel.GetType()
                            } instead of CustomBuyingPowerModel");
                    }

                    // Check that contracts slippage model is our custom slippage model
                    if (optionContract.SlippageModel.GetType() != typeof(CustomSlippageModel))
                    {
                        throw new Exception($@"Contract {optionContract.Symbol} has slippage model {optionContract.SlippageModel.GetType()
                            } instead of CustomSlippageModel");
                    }

                    // Check that contracts volatility model is our custom volatility model
                    if (optionContract.VolatilityModel.GetType() != typeof(CustomVolatilityModel))
                    {
                        throw new Exception($@"Contract {optionContract.Symbol} has volatility model {optionContract.VolatilityModel.GetType()
                            } instead of CustomVolatilityModel");
                    }

                    _modelsChecked = true;
                }
            }
        }

        public override void OnEndOfAlgorithm()
        {
            if (!_modelsChecked)
            {
                throw new Exception("Option contracts models were not checked.");
            }
        }

        protected virtual Option InitializeAlgorithm()
        {
            SetStartDate(2015, 12, 24);
            SetEndDate(2015, 12, 24);

            var equity = AddEquity("GOOG", leverage: 4);
            var option = AddOption(equity.Symbol);
            option.SetFilter(u => u.Strikes(-2, +2).Expiration(0, 180));

            SetBenchmark(x => 0);

            return option;
        }

        protected virtual void SetModels(Option option)
        {
            option.SetFillModel(new CustomFillModel());
            option.SetFeeModel(new CustomFeeModel());
            option.SetBuyingPowerModel(new CustomBuyingPowerModel());
            option.SetSlippageModel(new CustomSlippageModel());
            option.SetVolatilityModel(new CustomVolatilityModel());
            option.SettlementModel = new CustomSettlementModel();
        }

        public class CustomFillModel : FillModel
        {
        }

        public class CustomFeeModel : FeeModel
        {
        }

        public class CustomBuyingPowerModel : BuyingPowerModel
        {
        }

        public class CustomSlippageModel : ConstantSlippageModel
        {
            public CustomSlippageModel() : base(0)
            {
            }
        }

        public class CustomVolatilityModel : BaseVolatilityModel
        {
        }

        public class CustomSettlementModel : ImmediateSettlementModel
        {
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally => true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 475777;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
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
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
