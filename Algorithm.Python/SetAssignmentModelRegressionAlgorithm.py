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
from QuantConnect.Algorithm.CSharp import *

### <summary>
### Regression algorithm asserting that the correct market simulation instance is used when setting it using IAlgorithm.SetAssignmentModel
### </summary>
class SetAssignmentModelRegressionAlgorithm(QCAlgorithm):

    def Initialize(self):
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 28);
        self.SetCash(100000)

        self.AddEquity("SPY", Resolution.Hour)

        self._simulateMarketConditionsCallCount = 0

        try:
            self.SetAssignmentModel(None)
            raise Exception("Expected SetAssignmentModel to throw an exception when passed null")
        except ArgumentNullException:
            # expected
            pass

        assignmentModel = TestAssignmentModel()
        assignmentModel.OnSimulate += self._incrementSimulationCount
        self.SetAssignmentModel(assignmentModel)

    def OnEndOfAlgorithm(self):
        if self._incrementSimulationCount == 0:
            raise Exception("The market simulation was never used")

    def _incrementSimulationCount(self, sender, args):
        self._simulateMarketConditionsCallCount += 1
