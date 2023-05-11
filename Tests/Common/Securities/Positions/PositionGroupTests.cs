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

using NUnit.Framework;

using QuantConnect.Securities.Positions;
using QuantConnect.Securities.Option;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Common.Securities.Positions
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class PositionGroupTests
    {
        [TestCase(10, new[] { 1, 5 }, new[] { 10 * 1, 10 * 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { 1, 5 }, new[] { -10 * 1, -10 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { -1, 5 }, new[] { 10 * -1, 10 * 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { -1, 5 }, new[] { -10 * -1, -10 * 5 }, new[] { 1, 5 })]
        public void PositionGroupCreation(int groupQuantity, int[] positionsUnitQuantities, int[]expectedPositionsQuantities,
            int[] expectedPositionsUnitQuantities)
        {
            var symbols = GetSymbols(positionsUnitQuantities.Length);
            var group = CreatePositionGroup(groupQuantity, symbols, positionsUnitQuantities);
            var expectedPositions = symbols
                .Select((symbol, i) => new Position(symbol, expectedPositionsQuantities[i], expectedPositionsUnitQuantities[i]))
                .Cast<IPosition>()
                .ToList();

            AssertPositionGroup(group, groupQuantity, expectedPositions);
        }

        [TestCase(10, new[] { 1, 5 }, new[] { 1, 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { 1, 5 }, new[] { -1, -5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { -1, 5 }, new[] { -1, 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { -1, 5 }, new[] { 1, -5 }, new[] { 1, 5 })]
        public void CreatesUnitGroup(int groupQuantity, int[] positionsUnitQuantities, int[] expectedPositionsQuantities,
            int[] expectedPositionsUnitQuantities)
        {
            var symbols = GetSymbols(positionsUnitQuantities.Length);
            var group = CreatePositionGroup(groupQuantity, symbols, positionsUnitQuantities);

            var unitGroup = group.CreateUnitGroup();
            var expectedPositions = symbols
                .Select((symbol, i) => new Position(symbol, expectedPositionsQuantities[i], expectedPositionsUnitQuantities[i]))
                .Cast<IPosition>()
                .ToList();

            AssertPositionGroup(unitGroup, 1, expectedPositions);

            // Unit quantity for each position should be the same regardless of the group quantity
            var expectedTemplatePositions = symbols
                .Select((symbol, i) => new Position(symbol, groupQuantity * positionsUnitQuantities[i], expectedPositionsUnitQuantities[i]))
                .Cast<IPosition>()
                .ToList();
            AssertPositionGroup(group, groupQuantity, expectedTemplatePositions);
        }

        [TestCase(10, new[] { 1, 5 }, 1, new[] { 1 * 1, 1 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { 1, 5 }, -1, new[] { -1 * 1, -1 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { 1, 5 }, 50, new[] { 50 * 1, 50 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { 1, 5 }, -50, new[] { -50 * 1, -50 * 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { 1, 5 }, 1, new[] { 1 * -1, 1 * -5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { 1, 5 }, -1, new[] { -1 * -1, -1 * -5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { 1, 5 }, 50, new[] { 50 * -1, 50 * -5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { 1, 5 }, -50, new[] { -50 * -1, -50 * -5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { -1, 5 }, 1, new[] { 1 * -1, 1 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { -1, 5 }, -1, new[] { -1 * -1, -1 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { -1, 5 }, 50, new[] { 50 * -1, 50 * 5 }, new[] { 1, 5 })]
        [TestCase(10, new[] { -1, 5 }, -50, new[] { -50 * -1, -50 * 5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { -1, 5 }, 1, new[] { 1 * 1, 1 * -5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { -1, 5 }, -1, new[] { -1 * 1, -1 * -5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { -1, 5 }, 50, new[] { 50 * 1, 50 * -5 }, new[] { 1, 5 })]
        [TestCase(-10, new[] { -1, 5 }, -50, new[] { -50 * 1, -50 * -5 }, new[] { 1, 5 })]
        public void CreatesGroupWithQuantityFromTemplate(int groupQuantity, int[] positionsUnitQuantities, int newGroupQuantity,
            int[] expectedUnitGroupPositionsQuantities, int[] expectedPositionsUnitQuantities)
        {
            var symbols = GetSymbols(positionsUnitQuantities.Length);
            var group = CreatePositionGroup(groupQuantity, symbols, positionsUnitQuantities);

            var newGroup = group.WithQuantity(newGroupQuantity);
            var expectedPositions = symbols
                .Select((symbol, i) => new Position(symbol, expectedUnitGroupPositionsQuantities[i], expectedPositionsUnitQuantities[i]))
                .Cast<IPosition>()
                .ToList();

            AssertPositionGroup(newGroup, newGroupQuantity, expectedPositions);

            // Unit quantity for each position should be the same regardless of the group quantity
            var expectedTemplatePositions = symbols
                .Select((symbol, i) => new Position(symbol, groupQuantity * positionsUnitQuantities[i], expectedPositionsUnitQuantities[i]))
                .Cast<IPosition>()
                .ToList();
            AssertPositionGroup(group, groupQuantity, expectedTemplatePositions);
        }

        private static List<Symbol> GetSymbols(int count)
        {
            var baseExpiry = new DateTime(2023, 05, 19);
            return Enumerable.Range(0, count).Select(i => Symbols.CreateOptionSymbol("SPY", OptionRight.Call, 300, baseExpiry.AddMonths(i))).ToList();
        }

        private static IPositionGroup CreatePositionGroup(int quantity, List<Symbol> symbols, int[] positionsUnitQuantities)
        {
            Assert.IsNotEmpty(positionsUnitQuantities);
            Assert.AreEqual(positionsUnitQuantities.Length, symbols.Count);

            return new PositionGroup(
                new OptionStrategyPositionGroupBuyingPowerModel(null),
                positionsUnitQuantities
                    .Select((positionUnitQuantity, i) => new Position(symbols[i], quantity * positionUnitQuantity, Math.Abs(positionUnitQuantity)))
                    .ToArray());
        }

        /// <summary>
        /// Asserts that the specified group has the expected quantity and positions
        /// </summary>
        private static void AssertPositionGroup(IPositionGroup group, int expectedQuantity, List<IPosition> expectedPositions)
        {
            Assert.AreEqual(Math.Abs(expectedQuantity), Math.Abs(group.Quantity));
            Assert.AreEqual(expectedPositions.Count, group.Count);

            foreach (var expectedPosition in expectedPositions)
            {
                var position = group.GetPosition(expectedPosition.Symbol);
                Assert.AreEqual(expectedPosition.Quantity, position.Quantity);
                Assert.AreEqual(expectedPosition.UnitQuantity, position.UnitQuantity);
            }
        }
    }
}
