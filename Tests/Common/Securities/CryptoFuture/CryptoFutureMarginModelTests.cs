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
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.CryptoFuture;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities.CryptoFuture
{
    [TestFixture]
    public class CryptoFutureMarginModelTests
    {
        [TestCase("BTCUSD")]
        [TestCase("BTCUSDT")]
        public void DefaultMarginModelType(string ticker)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);

            Assert.AreEqual(typeof(CryptoFutureMarginModel), cryptoFuture.BuyingPowerModel.GetType());
        }

        [TestCase("BTCUSD", 10)]
        [TestCase("BTCUSDT", 10)]
        [TestCase("BTCUSD", -10)]
        [TestCase("BTCUSDT", -10)]
        public void InitialMarginRequirement(string ticker, decimal quantity)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, 16000);

            var parameters = new InitialMarginParameters(cryptoFuture, quantity);
            var result = cryptoFuture.BuyingPowerModel.GetInitialMarginRequirement(parameters);

            decimal marginRequirement;
            if (ticker == "BTCUSD")
            {
                // ((quantity * contract mutiplier * price) / leverage) * conversion rate (BTC -> USD)
                marginRequirement = ((parameters.Quantity * 100m * cryptoFuture.Price) / 25m ) *  1 / cryptoFuture.Price;
            }
            else
            {
                // ((quantity * contract mutiplier * price) / leverage) * conversion rate (USDT ~= USD)
                marginRequirement = ((parameters.Quantity * 1m * cryptoFuture.Price) / 25m) * 1;
            }

            Assert.AreEqual(Math.Abs(marginRequirement), result.Value);
        }

        [TestCase("BTCUSD", 10)]
        [TestCase("BTCUSDT", 10)]
        [TestCase("BTCUSD", -10)]
        [TestCase("BTCUSDT", -10)]
        public void GetMaintenanceMargin(string ticker, decimal quantity)
        {
            var algo = GetAlgorithm();
            var cryptoFuture = algo.AddCryptoFuture(ticker);
            SetPrice(cryptoFuture, 16000);
            // entry price 1000, shouldn't matter
            cryptoFuture.Holdings.SetHoldings(1000, quantity);

            var parameters = MaintenanceMarginParameters.ForCurrentHoldings(cryptoFuture);
            var result = cryptoFuture.BuyingPowerModel.GetMaintenanceMargin(parameters);

            decimal marginRequirement;
            if (ticker == "BTCUSD")
            {
                // ((quantity * contract mutiplier * price) * MaintenanceMarginRate) * conversion rate (BTC -> USD)
                marginRequirement = ((parameters.Quantity * 100m * cryptoFuture.Price) * 0.05m) * 1 / cryptoFuture.Price;
            }
            else
            {
                // ((quantity * contract mutiplier * price) * MaintenanceMarginRate) * conversion rate (USDT ~= USD)
                marginRequirement = ((parameters.Quantity * 1m * cryptoFuture.Price) * 0.05m) * 1;
            }

            Assert.AreEqual(Math.Abs(marginRequirement), result.Value);
        }

        [TestCase(PositionSide.Long, PositionSide.None)]
        [TestCase(PositionSide.Long, PositionSide.Long)]
        [TestCase(PositionSide.Long, PositionSide.Short)]
        [TestCase(PositionSide.Short, PositionSide.None)]
        [TestCase(PositionSide.Short, PositionSide.Long)]
        [TestCase(PositionSide.Short, PositionSide.Short)]
        public void ConsidersOpenPositionInRemainingMarginCalculationForSameDirectionOrder(PositionSide initialPositionSide,
            PositionSide otherCryptoFutureInitialPositionSide)
        {
            var algo = GetAlgorithm();
            var initialCash = 10000;
            algo.SetCash("USDT", initialCash);

            var cryptoFuture = algo.AddCryptoFuture("BTCUSDT");
            cryptoFuture.SetFeeModel(new ConstantFeeModel(0));
            // Price is the same as the initial cash
            SetPrice(cryptoFuture, initialCash);

            var marginModel = new TestableCryptoFutureMarginModel(10);
            cryptoFuture.SetBuyingPowerModel(marginModel);

            // We should have the initial cash as remaining margin
            foreach (var direction in new[] { OrderDirection.Buy, OrderDirection.Sell })
            {
                Assert.AreEqual(initialCash, marginModel.GetMarginRemainingPublic(algo.Portfolio, cryptoFuture, direction),
                    $"Failed with order direction {direction}");
            }

            var otherCryptoMargin = 0m;
            if (otherCryptoFutureInitialPositionSide != PositionSide.None)
            {
                // Let's add some other holdings
                var otherCryptoFuture = algo.AddCryptoFuture("ADAUSDT");
                otherCryptoFuture.SetFeeModel(new ConstantFeeModel(0));
                otherCryptoFuture.SetLeverage(100);
                SetPrice(otherCryptoFuture, 1000);
                var sign = otherCryptoFutureInitialPositionSide == PositionSide.Long ? 1 : -1;
                otherCryptoFuture.Holdings.SetHoldings(otherCryptoFuture.Price, sign * 100);

                // Margin is adjusted by leverage in Lean
                otherCryptoMargin = 100 * 1000 / otherCryptoFuture.Leverage;

                // We should have the initial cash minus the margin used by the other holdings
                foreach (var direction in new[] { OrderDirection.Buy, OrderDirection.Sell })
                {
                    Assert.AreEqual(initialCash - otherCryptoMargin, marginModel.GetMarginRemainingPublic(algo.Portfolio, cryptoFuture, direction),
                        $"Failed with order direction {direction}");
                }
            }

            // With leverage 10, price 10000, and initial cash 10000, the max holdings quantity would be 10 (10000 * 10 / 10000).
            // Let's set holdings to half of the max quantity
            var holdingsSign = initialPositionSide == PositionSide.Long ? 1 : -1;
            cryptoFuture.Holdings.SetHoldings(cryptoFuture.Price, holdingsSign * cryptoFuture.Leverage / 2);

            // We should have 5000 remaining margin, since the initial margin requirement
            // for the current holdings quantity (5) with the current leverage (10) is 5000
            var orderDirection = initialPositionSide == PositionSide.Long ? OrderDirection.Buy : OrderDirection.Sell;
            var remainingMargin = marginModel.GetMarginRemainingPublic(algo.Portfolio, cryptoFuture, orderDirection);
            Assert.AreEqual(initialCash - initialCash / 2 - otherCryptoMargin, remainingMargin);

            // Let's double the leverage
            cryptoFuture.SetLeverage(20);

            // We should have 7500 remaining margin, since the initial margin requirement
            // for the current holdings quantity (5) with the new leverage (20) is 2500
            remainingMargin = marginModel.GetMarginRemainingPublic(algo.Portfolio, cryptoFuture, orderDirection);
            Assert.AreEqual(initialCash - initialCash / 4 - otherCryptoMargin, remainingMargin);
        }

        private static QCAlgorithm GetAlgorithm()
        {
            // Initialize algorithm
            var algo = new AlgorithmStub();
            algo.SetFinishedWarmingUp();
            return algo;
        }

        private static void SetPrice(Security security, decimal price)
        {
            var cryptoFuture = (QuantConnect.Securities.CryptoFuture.CryptoFuture) security;
            cryptoFuture.BaseCurrency.ConversionRate = price;
            cryptoFuture.QuoteCurrency.ConversionRate = 1;

            security.SetMarketPrice(new TradeBar
            {
                Time = new DateTime(2022, 12, 22),
                Symbol = security.Symbol,
                Open = price,
                High = price,
                Low = price,
                Close = price
            });
        }

        private class TestableCryptoFutureMarginModel : CryptoFutureMarginModel
        {
            public TestableCryptoFutureMarginModel(decimal leverage = 25, decimal maintenanceMarginRate = 0.05m, decimal maintenanceAmount = 0)
                : base(leverage, maintenanceMarginRate, maintenanceAmount)
            {
            }

            public decimal GetMarginRemainingPublic(SecurityPortfolioManager portfolio, Security security, OrderDirection direction)
            {
                return GetMarginRemaining(portfolio, security, direction);
            }
        }
    }
}
