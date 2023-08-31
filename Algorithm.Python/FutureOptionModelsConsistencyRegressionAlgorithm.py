# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

from AlgorithmImports import *

from OptionModelsConsistencyRegressionAlgorithm import OptionModelsConsistencyRegressionAlgorithm

### <summary>
### Regression algorithm asserting that models set to a future option are passed down to the chain contracts
### </summary>
class FutureOptionModelsConsistencyRegressionAlgorithm(OptionModelsConsistencyRegressionAlgorithm):

    def Initialize(self) -> None:
        self.SetStartDate(2012, 1, 3)
        self.SetEndDate(2012, 1, 4)

        dc_contract = self.AddFutureContract(
            Symbol.CreateFuture(
                Futures.Dairy.ClassIIIMilk,
                Market.CME,
                datetime(2012, 4, 1)),
            Resolution.Daily)

        option_symbol = list(self.OptionChainProvider.GetOptionContractList(dc_contract.Symbol, self.Time))[0]

        self.AddFutureOption(option_symbol.Canonical)
        self.SetModels(self.Securities[option_symbol.Canonical])

        option_contract = self.AddFutureOptionContract(option_symbol, Resolution.Daily)

        self.CheckModels(option_contract)

    def OnData(self, slice: Slice) -> None:
        # Empty, we just don't want the base class checks done in OnData since we are adding the option contract in Initialize
        # and checking the models.
        pass
