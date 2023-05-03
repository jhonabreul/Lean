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
using NUnit.Framework;

using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Securities.Option;
using QuantConnect.Securities.Option.StrategyMatcher;
using QuantConnect.Securities.Positions;
using QuantConnect.Tests.Engine.DataFeeds;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class OptionStrategyPositionGroupBuyingPowerModelTests
    {
        private QCAlgorithm _algorithm;
        private SecurityPortfolioManager _portfolio;
        private QuantConnect.Securities.Equity.Equity _equity;
        private Option _callOption;
        private Option _putOption;

        [SetUp]
        public void Setup()
        {
            _algorithm = new AlgorithmStub();
            _algorithm.SetCash(100000);
            _algorithm.SetSecurityInitializer(security => security.FeeModel = new ConstantFeeModel(0));
            _portfolio = _algorithm.Portfolio;

            _equity = _algorithm.AddEquity("SPY");

            var strike = 200m;
            var expiry = new DateTime(2016, 1, 15);

            var callOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Call, strike, expiry);
            _callOption = _algorithm.AddOptionContract(callOptionSymbol);

            var putOptionSymbol = Symbols.CreateOptionSymbol("SPY", OptionRight.Put, strike, expiry);
            _putOption = _algorithm.AddOptionContract(putOptionSymbol);
        }

        [Test]
        public void HasSufficientBuyingPowerForStrategyOrder([Values] bool withInitialHoldings)
        {
            const decimal price = 1.2345m;
            const decimal underlyingPrice = 200m;

            var initialMargin = _portfolio.MarginRemaining;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            var initialHoldingsQuantity = withInitialHoldings ? -10 : 0;
            _callOption.Holdings.SetHoldings(1.5m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            var optionStrategy = OptionStrategies.Straddle(_callOption.Symbol.Canonical, _callOption.StrikePrice, _callOption.Expiry);

            var sufficientCaseConsidered = false;
            var insufficientCaseConsidered = false;

            // make sure these cases are considered:
            // 1. liquidating part of the position
            var partialLiquidationCaseConsidered = false;
            // 2. liquidating the whole position
            var fullLiquidationCaseConsidered = false;
            // 3. shorting more, but with margin left
            var furtherShortingWithMarginRemainingCaseConsidered = false;
            // 4. shorting even more to the point margin is no longer enough
            var furtherShortingWithNoMarginRemainingCaseConsidered = false;

            for (var strategyQuantity = Math.Abs(initialHoldingsQuantity); strategyQuantity > -30; strategyQuantity--)
            {
                var buyingPowerModel = new OptionStrategyPositionGroupBuyingPowerModel(
                    _callOption.Holdings.Quantity + strategyQuantity == 0
                        // Liquidating
                        ? null
                        : optionStrategy);
                var orders = GetStrategyOrders(strategyQuantity);

                var positionGroup = _portfolio.Positions.CreatePositionGroup(orders);

                var maintenanceMargin = buyingPowerModel.GetMaintenanceMargin(
                    new PositionGroupMaintenanceMarginParameters(_portfolio, positionGroup));

                var hasSufficientBuyingPowerResult = buyingPowerModel.HasSufficientBuyingPowerForOrder(
                    new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

                Assert.AreEqual(maintenanceMargin < initialMargin, hasSufficientBuyingPowerResult.IsSufficient);

                if (hasSufficientBuyingPowerResult.IsSufficient)
                {
                    sufficientCaseConsidered = true;
                }
                else
                {
                    Assert.IsTrue(sufficientCaseConsidered, "All 'sufficient buying power' case should have been before the 'insufficient' ones");

                    insufficientCaseConsidered = true;
                }

                var newPositionQuantity = positionGroup.Quantity;
                if (newPositionQuantity == 0)
                {
                    fullLiquidationCaseConsidered = true;
                }
                else if (newPositionQuantity < 0)
                {
                    if (newPositionQuantity > initialHoldingsQuantity)
                    {
                        partialLiquidationCaseConsidered = true;
                    }
                    else if (hasSufficientBuyingPowerResult.IsSufficient)
                    {
                        furtherShortingWithMarginRemainingCaseConsidered = true;
                    }
                    else
                    {
                        furtherShortingWithNoMarginRemainingCaseConsidered = true;
                    }
                }
            }

            Assert.IsTrue(sufficientCaseConsidered, "The 'sufficient buying power' case was not considered");
            Assert.IsTrue(insufficientCaseConsidered, "The 'insufficient buying power' case was not considered");

            if (withInitialHoldings)
            {
                Assert.IsTrue(partialLiquidationCaseConsidered, "The 'partial liquidation' case was not considered");
                Assert.IsTrue(fullLiquidationCaseConsidered, "The 'full liquidation' case was not considered");
            }

            Assert.IsTrue(furtherShortingWithMarginRemainingCaseConsidered, "The 'further shorting with margin remaining' case was not considered");
            Assert.IsTrue(furtherShortingWithNoMarginRemainingCaseConsidered, "The 'further shorting with no margin remaining' case was not considered");
        }

        [Test]
        public void HasSufficientBuyingPowerForReducingStrategyOrder()
        {
            const decimal price = 1m;
            const decimal underlyingPrice = 200m;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            var initialHoldingsQuantity = -10;
            _callOption.Holdings.SetHoldings(1.5m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            _algorithm.SetCash(_portfolio.TotalMarginUsed * 0.95m);

            var optionStrategy = OptionStrategies.Straddle(_callOption.Symbol.Canonical, _callOption.StrikePrice, _callOption.Expiry);
            var quantity = -initialHoldingsQuantity / 2;
            var buyingPowerModel = new OptionStrategyPositionGroupBuyingPowerModel(optionStrategy);
            var orders = GetStrategyOrders(quantity);

            var positionGroup = _portfolio.Positions.CreatePositionGroup(orders);

            var parameters = new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders);
            var availableBuyingPower = buyingPowerModel.GetPositionGroupBuyingPower(parameters.Portfolio, parameters.PositionGroup, orders.First().GroupOrderManager.Direction);
            var deltaBuyingPowerArgs = new ReservedBuyingPowerImpactParameters(parameters.Portfolio, parameters.PositionGroup, parameters.Orders);
            var deltaBuyingPower = buyingPowerModel.GetReservedBuyingPowerImpact(deltaBuyingPowerArgs).Delta;

            // Buying power should be sufficient for reducing the position, even if the delta buying power is greater than the available buying power
            Assert.Less(deltaBuyingPower, 0);
            Assert.Greater(deltaBuyingPower, availableBuyingPower);

            var hasSufficientBuyingPowerResult = buyingPowerModel.HasSufficientBuyingPowerForOrder(
                new HasSufficientPositionGroupBuyingPowerForOrderParameters(_portfolio, positionGroup, orders));

            Assert.IsTrue(hasSufficientBuyingPowerResult.IsSufficient);
        }

        // Going even shorter
        [TestCase(-10, -11)]
        // Going "less" short
        [TestCase(-10, -9)]
        // Liquidating
        [TestCase(-10, 0)]
        // Going long from short
        [TestCase(-10, 10)]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPowerFromShortPosition(int initialHoldingsQuantity, int finalPositionQuantity)
        {
            // Just making sure we start from a short position
            var absQuantity = Math.Abs(initialHoldingsQuantity);
            initialHoldingsQuantity = -absQuantity;

            SetUpOptionStrategy(initialHoldingsQuantity);
            var positionGroup = _portfolio.PositionGroups.Single();

            var expectedQuantity = finalPositionQuantity - initialHoldingsQuantity;
            var usedMargin = _portfolio.TotalMarginUsed;
            var marginPerNakedShortUnit = usedMargin / absQuantity;

            var longUnitGroup = positionGroup.Key.CreateUnitGroup();
            var marginPerLongUnit = longUnitGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, longUnitGroup)).Value;

            var deltaBuyingPower = usedMargin + finalPositionQuantity * (finalPositionQuantity < 0 ? marginPerNakedShortUnit : marginPerLongUnit);

            ComputeAndAssertQuantityForDeltaBuyingPower(positionGroup, expectedQuantity, deltaBuyingPower);
        }

        // Going even longer
        [TestCase(10, 11)]
        // Going "less" long
        [TestCase(10, 9)]
        // Liquidating
        [TestCase(10, 0)]
        // Going short from long
        [TestCase(10, -10)]
        public void PositionGroupOrderQuantityCalculationForDeltaBuyingPowerFromLongPosition(int initialHoldingsQuantity, int finalPositionQuantity)
        {
            // Just making sure we start from a long position
            initialHoldingsQuantity = Math.Abs(initialHoldingsQuantity);

            SetUpOptionStrategy(initialHoldingsQuantity);
            var positionGroup = _portfolio.PositionGroups.Single();

            var expectedQuantity = finalPositionQuantity - initialHoldingsQuantity;
            var usedMargin = _portfolio.TotalMarginUsed;
            var marginPerLongUnit = usedMargin / initialHoldingsQuantity;

            var shortUnitGroup = positionGroup.WithQuantity(-1);
            var marginPerNakedShortUnit = shortUnitGroup.BuyingPowerModel.GetInitialMarginRequirement(
                new PositionGroupInitialMarginParameters(_portfolio, shortUnitGroup)).Value;

            var deltaBuyingPower = finalPositionQuantity >= 0
                //Going even longer / Going "less" long/ Liquidating
                ? expectedQuantity * marginPerLongUnit
                // Going short from long
                : -usedMargin + Math.Abs(finalPositionQuantity) * marginPerNakedShortUnit;

            ComputeAndAssertQuantityForDeltaBuyingPower(positionGroup, expectedQuantity, deltaBuyingPower);
        }

        [TestCase(-10, 1, +1)]
        [TestCase(-10, 1, -1)]
        [TestCase(-10, 2, +1)]
        [TestCase(-10, 2, -1)]
        [TestCase(-10, 0.5, +1)]
        [TestCase(-10, 0.5, -1)]
        [TestCase(10, 1, +1)]
        [TestCase(10, 1, -1)]
        [TestCase(10, 2, +1)]
        [TestCase(10, 2, -1)]
        [TestCase(10, 0.5, +1)]
        [TestCase(10, 0.5, -1)]
        public void OrderQuantityCalculation(int initialHoldingsQuantity, decimal targetMarginPercent, int targetMarginDirection)
        {
            // The targetMarginDirection whether we want to go in the same direction as the holdings:
            //   +1: short will go shorter and long will go longer
            //   -1: short will go towards long and vice-versa

            SetUpOptionStrategy(initialHoldingsQuantity);
            var positionGroup = _portfolio.PositionGroups.Single();

            var expectedQuantity = Math.Sign(targetMarginDirection) * initialHoldingsQuantity * targetMarginPercent;
            var finalPositionQuantity = initialHoldingsQuantity + expectedQuantity;

            var buyingPowerModel = positionGroup.BuyingPowerModel as PositionGroupBuyingPowerModel;

            var longUnitGroup = positionGroup.Key.CreateUnitGroup();
            var longUnitMargin = buyingPowerModel.GetInitialMarginRequirement(_portfolio, longUnitGroup);

            var shortUnitGroup = positionGroup.WithQuantity(-1);
            var shortUnitMargin = buyingPowerModel.GetInitialMarginRequirement(_portfolio, shortUnitGroup);

            var targetFinalMargin = finalPositionQuantity < 0
                // Final position will be short
                ? -finalPositionQuantity * shortUnitMargin
                // Final position will be long (or closed)
                : finalPositionQuantity * longUnitMargin;

            var currentUsedMargin = buyingPowerModel.GetInitialMarginRequirement(_portfolio, positionGroup);

            var quantity = buyingPowerModel.GetPositionGroupOrderQuantity(_portfolio, positionGroup, currentUsedMargin, targetFinalMargin,
                longUnitGroup, longUnitMargin, out _);

            // Reducing the position or going shorter
            if (targetFinalMargin < currentUsedMargin)
            {
                Assert.Less(quantity, 0);
            }
            // Increasing the position
            else if (targetFinalMargin >= currentUsedMargin)
            {
                Assert.Greater(quantity, 0);
            }

            // Liquidating
            if (targetFinalMargin == 0)
            {
                Assert.AreEqual(-initialHoldingsQuantity, quantity);
            }

            Assert.AreEqual(expectedQuantity, quantity);
        }

        private List<Order> GetStrategyOrders(decimal quantity)
        {
            var groupOrderManager = new GroupOrderManager(1, 2, quantity);
            return new List<Order>()
            {
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _callOption.Type,
                    _callOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager)),
                Order.CreateOrder(new SubmitOrderRequest(
                    OrderType.ComboMarket,
                    _putOption.Type,
                    _putOption.Symbol,
                    1m.GetOrderLegGroupQuantity(groupOrderManager),
                    0,
                    0,
                    _algorithm.Time,
                    "",
                    groupOrderManager: groupOrderManager))
            };
        }

        private void SetUpOptionStrategy(int initialHoldingsQuantity)
        {
            const decimal price = 1.5m;
            const decimal underlyingPrice = 300m;

            _equity.SetMarketPrice(new Tick { Value = underlyingPrice });
            _callOption.SetMarketPrice(new Tick { Value = price });
            _putOption.SetMarketPrice(new Tick { Value = price });

            _callOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);
            _putOption.Holdings.SetHoldings(1m, initialHoldingsQuantity);

            Assert.AreEqual(1, _portfolio.PositionGroups.Count);

            var positionGroup = _portfolio.PositionGroups.First();
            Assert.AreEqual(OptionStrategyDefinitions.Straddle.Name, positionGroup.BuyingPowerModel.ToString());

            var callOptionPosition = positionGroup.Positions.Single(x => x.Symbol == _callOption.Symbol);
            Assert.AreEqual(initialHoldingsQuantity, callOptionPosition.Quantity);

            var putOptionPosition = positionGroup.Positions.Single(x => x.Symbol == _putOption.Symbol);
            Assert.AreEqual(initialHoldingsQuantity, putOptionPosition.Quantity);
        }

        private void ComputeAndAssertQuantityForDeltaBuyingPower(IPositionGroup positionGroup, decimal expectedQuantity, decimal deltaBuyingPower)
        {
            var quantity = positionGroup.BuyingPowerModel.GetMaximumLotsForDeltaBuyingPower(new GetMaximumLotsForDeltaBuyingPowerParameters(
                _portfolio, positionGroup, deltaBuyingPower, minimumOrderMarginPortfolioPercentage: 0)).NumberOfLots;

            Assert.AreEqual(expectedQuantity, quantity);
        }
    }
}
