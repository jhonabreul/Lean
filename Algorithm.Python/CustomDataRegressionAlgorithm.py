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

import json
from AlgorithmImports import *

### <summary>
### Regression test to demonstrate importing and trading on custom data.
### </summary>
### <meta name="tag" content="using data" />
### <meta name="tag" content="importing data" />
### <meta name="tag" content="custom data" />
### <meta name="tag" content="crypto" />
### <meta name="tag" content="regression test" />
class CustomDataRegressionAlgorithm(QCAlgorithm):

    def initialize(self) -> None:
        self.set_start_date(2020,1,5)   # Set Start Date
        self.set_end_date(2020,1,10)     # Set End Date
        self.set_cash(100000)           # Set Strategy Cash

        resolution = Resolution.SECOND if self.live_mode else Resolution.DAILY
        self.add_data(Bitcoin, "BTC", resolution)

        seeder = FuncSecuritySeeder(self.get_last_known_prices)
        self.set_security_initializer(lambda x: seeder.seed_security(x))
        self._warmed_up_checked = False

    def on_data(self, data: Slice) -> None:
        if not self.portfolio.invested:
            if data['BTC'].close != 0 :
                self.order('BTC', self.portfolio.margin_remaining/abs(data['BTC'].close + 1))

    def on_securities_changed(self, changes: SecurityChanges) -> None:
        changes.filter_custom_securities = False
        for added_security in changes.added_securities:
            if added_security.symbol.value == "BTC":
                self._warmed_up_checked = True
            if not added_security.has_data:
                raise ValueError(f"Security {added_security.symbol} was not warmed up!")

    def on_end_of_algorithm(self) -> None:
        if not self._warmed_up_checked:
            raise ValueError("Security was not warmed up!")

class Bitcoin(PythonData):
    '''Custom Data Type: Bitcoin data from Quandl - https://data.nasdaq.com/databases/BCHAIN'''

    def get_source(self, config: SubscriptionDataConfig, date: datetime, is_live_mode: bool) -> SubscriptionDataSource:
        if is_live_mode:
            return SubscriptionDataSource("https://www.bitstamp.net/api/ticker/", SubscriptionTransportMedium.REST)

        #return "http://my-ftp-server.com/futures-data-" + date.to_string("Ymd") + ".zip"
        # OR simply return a fixed small data file. Large files will slow down your backtest
        subscription = SubscriptionDataSource("https://www.quantconnect.com/api/v2/proxy/nasdaq/api/v3/datatables/QDL/BITFINEX.csv?code=BTCUSD&api_key=WyAazVXnq7ATy_fefTqm")
        subscription.sort = True
        return subscription

    def reader(self, config: SubscriptionDataConfig, line: str, date: datetime, is_live_mode: bool) -> DynamicData:
        coin = Bitcoin()
        coin.symbol = config.symbol

        if is_live_mode:
            # Example Line Format:
            # {"high": "441.00", "last": "421.86", "timestamp": "1411606877", "bid": "421.96", "vwap": "428.58", "volume": "14120.40683975", "low": "418.83", "ask": "421.99"}
            try:
                live_btc = json.loads(line)

                # If value is zero, return coin
                value = live_btc["last"]
                if value == 0:
                    return coin

                coin.time = datetime.now()
                coin.value = value
                coin["Open"] = float(live_btc["open"])
                coin["High"] = float(live_btc["high"])
                coin["Low"] = float(live_btc["low"])
                coin["Close"] = float(live_btc["last"])
                coin["Ask"] = float(live_btc["ask"])
                coin["Bid"] = float(live_btc["bid"])
                coin["VolumeBTC"] = float(live_btc["volume"])
                coin["WeightedPrice"] = float(live_btc["vwap"])
                return coin
            except ValueError:
                # Do nothing, possible error in json decoding
                return coin

        # Example Line Format:
        # code    date        high     low      mid      last     bid      ask      volume
        # BTCUSD  2024-10-08  63248.0  61940.0  62246.5  62245.0  62246.0  62247.0  477.91102114
        if not (line.strip() and line[7].isdigit()): return coin

        try:
            data = line.split(',')
            coin.time = datetime.strptime(data[1], "%Y-%m-%d")
            coin.end_time = coin.time + timedelta(days=1)
            coin.value = float(data[5])
            coin["High"] = float(data[2])
            coin["Low"] = float(data[3])
            coin["Mid"] = float(data[4])
            coin["Close"] = float(data[5])
            coin["Bid"] = float(data[6])
            coin["Ask"] = float(data[7])
            coin["VolumeBTC"] = float(data[8])
            return coin

        except ValueError:
            # Do nothing, possible error in json decoding
            return coin
