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

### <summary>
### Regression algorithm asserting that models set to a canonical option are passed down to the option chain contracts
### </summary>
class OptionModelsConsistencyRegressionAlgorithm(QCAlgorithm):

    def Initialize(self) -> None:
        security = self.InitializeAlgorithm()
        self.symbol = security.Symbol

        security.SetFillModel(CustomFillModel())
        security.SetFeeModel(CustomFeeModel())
        security.SetBuyingPowerModel(CustomBuyingPowerModel())
        security.SetSlippageModel(CustomSlippageModel())
        security.SetVolatilityModel(CustomVolatilityModel())

        # Using a custom security initializer derived from BrokerageModelSecurityInitializer
        # to check that the models are correctly set in the security even when the
        # security initializer is derived from said class in Python
        self.SetSecurityInitializer(CustomSecurityInitializer(self.BrokerageModel, SecuritySeeder.Null))

        self.SetBenchmark(lambda x: 0)

        self.models_checked = False

    def InitializeAlgorithm(self) -> Security:
        self.SetStartDate(2015, 12, 24)
        self.SetEndDate(2015, 12, 24)

        equity = self.AddEquity("GOOG", leverage=4)
        option = self.AddOption(equity.Symbol)
        option.SetFilter(lambda u: u.Strikes(-2, +2).Expiration(0, 180))

        return option

    def OnData(self, slice) -> None:
        chain = slice.OptionChains.get(self.symbol)
        if chain is not None:
            for contract in chain:
                optionContract = self.Securities[contract.Symbol]
                self.CheckModels(optionContract)

    def CheckModels(self, security: Security) -> None:
        # Check that contracts fill model is our custom fill model
        if list(security.FillModel.Fill(None))[0].Message != "Custom Fill Model":
            raise Exception(f"Contract {security.Symbol} fill model is not an instance of CustomFillModel")

        # Check that contracts fee model is our custom fee model
        fee = security.FeeModel.GetOrderFee(None)
        if fee.Value.Amount != 123 or fee.Value.Currency != "ABC":
            raise Exception(f"Contract {security.Symbol} fee model is not an instance of CustomFeeModel")

        # Check that contracts buying power model is our custom buying power model
        if security.BuyingPowerModel.GetLeverage(None) != 999:
            raise Exception(f"Contract {security.Symbol} buying power model is not an instance of CustomBuyingPowerModel")

        # Check that contracts slippage model is our custom slippage model
        if security.SlippageModel.GetSlippageApproximation(None, None) != 999:
            raise Exception(f"Contract {security.Symbol} slippage model is not an instance of CustomSlippageModel")

        # Check that contracts volatility model is our custom volatility model
        if security.VolatilityModel.Volatility != 999:
            raise Exception(f"Contract {security.Symbol} volatility model is not an instance of CustomVolatilityModel")

        self.models_checked = True

    def OnEndOfAlgorithm(self) -> None:
        if not self.models_checked:
            raise Exception("Models were not checked")

class CustomSecurityInitializer(BrokerageModelSecurityInitializer):
    def __init__(self, brokerage_model: BrokerageModel, security_seeder: SecuritySeeder):
        super().__init__(brokerage_model, security_seeder)

class CustomFillModel(FillModel):
    def Fill(self, parameters: FillModelParameters) -> Fill:
        fill = OrderEvent()
        fill.Message = "Custom Fill Model"
        return Fill(fill)

class CustomFeeModel(FeeModel):
    def GetOrderFee(self, parameters: OrderFeeParameters) -> OrderFee:
        return OrderFee(CashAmount(123, "ABC"))

class CustomBuyingPowerModel(BuyingPowerModel):
    def GetLeverage(self, security: Security) -> float:
        return 999

class CustomSlippageModel(ConstantSlippageModel):
    def __init__(self):
        super().__init__(0)

    def GetSlippageApproximation(self, asset: Security, order: Order) -> float:
        return 999

class CustomVolatilityModel(BaseVolatilityModel):
    Volatility = 999
