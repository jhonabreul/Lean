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
### Regression algorithm asserting that models set to a future are passed down to the mapped contracts
### </summary>
class ContinuousFutureModelsConsistencyRegressionAlgorithm(OptionModelsConsistencyRegressionAlgorithm):

    def InitializeAlgorithm(self) -> Security:
        self.SetStartDate(2013, 7, 1)
        self.SetEndDate(2014, 1, 1)

        self.continuousContract = self.AddFuture(Futures.Indices.SP500EMini,
                                                 dataNormalizationMode=DataNormalizationMode.BackwardsPanamaCanal,
                                                 dataMappingMode=DataMappingMode.OpenInterest,
                                                 contractDepthOffset=1)

        return self.continuousContract

    def OnData(self, slice) -> None:
        for changedEvent in slice.SymbolChangedEvents.values():
            if changedEvent.Symbol == self.continuousContract.Symbol:
                self.future_mapped = True
                self.CheckModels(self.Securities[self.continuousContract.Mapped])

    def OnEndOfAlgorithm(self) -> None:
        super().OnEndOfAlgorithm()

        if not self.future_mapped:
            raise Exception("No mappings were found for the continuous future.")
