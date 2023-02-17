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

using QuantConnect.Interfaces;

namespace QuantConnect.Tests.Research.RegressionTemplates
{
    /// <summary>
    /// Basic template framework for regression testing of research notebooks
    /// </summary>
    public class BasicTemplateCustomDataTypeHistoryResearchCSharp : IRegressionResearchDefinition
    {
        /// <summary>
        /// Expected output from the reading the raw notebook file
        /// </summary>
        /// <remarks>Requires to be implemented last in the file <see cref="ResearchRegressionTests.UpdateResearchRegressionOutputInSourceFile"/>
        /// get should start from next line</remarks>
        public string ExpectedOutput =>
            "{ \"cells\": [  {   \"cell_type\": \"markdown\",   \"id\": \"e28a23ae\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_time\"" +
            ": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"    },    \"tags\": []   },   \"source\": [    \"![QuantConnec" +
            "t Logo](https://cdn.quantconnect.com/web/i/icon.png)\",    \"<hr>\"   ]  },  {   \"cell_type\": \"markdown\",   \"id\": \"512dd871\",   \"metadata\": " +
            "{    \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"" +
            "    },    \"tags\": []   },   \"source\": [    \"# Custom data history\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": null,   \"id\": " +
            "\"bb0c5d0a\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\": null,     \"start_time\": null," +
            "     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"outputs\": [],   \"source\": [    " +
            "\"// We need to load assemblies at the start in their own cell\",    \"#load \\\"../Initialize.csx\\\"\"   ]  },  {   \"cell_type\": \"code\",   \"exe" +
            "cution_count\": null,   \"id\": \"f972f2ce\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\":" +
            " null,     \"start_time\": null,     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"ou" +
            "tputs\": [],   \"source\": [    \"// Initialize Lean Engine.\",    \"#load \\\"../QuantConnect.csx\\\"\",    \"\",    \"using System.Globalization;\"," +
            "    \"using QuantConnect;\",    \"using QuantConnect.Data;\",    \"using QuantConnect.Algorithm;\",    \"using QuantConnect.Research;\"   ]  },  {   \"" +
            "cell_type\": \"code\",   \"execution_count\": null,   \"id\": \"5b180ed7\",   \"metadata\": {    \"papermill\": {     \"duration\": null,     \"end_ti" +
            "me\": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"    },    \"tags\": [],    \"vscode\": {     \"languageId\"" +
            ": \"csharp\"    }   },   \"outputs\": [],   \"source\": [    \"class CustomDataType : DynamicData\",    \"{\",    \"    public decimal Open;\",    \" " +
            "   public decimal High;\",    \"    public decimal Low;\",    \"    public decimal Close;\",    \"\",    \"    public override SubscriptionDataSource " +
            "GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)\",    \"    {\",    \"        var source = \\\"https://www.dl.dropboxusercont" +
            "ent.com/s/d83xvd7mm9fzpk0/path_to_my_csv_data.csv?dl=0\\\";\",    \"        return new SubscriptionDataSource(source, SubscriptionTransportMedium.Remo" +
            "teFile);\",    \"    }\",    \"\",    \"    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode" +
            ")\",    \"    {\",    \"        if (string.IsNullOrWhiteSpace(line.Trim()))\",    \"        {\",    \"            return null;\",    \"        }\",   " +
            " \"\",    \"        try\",    \"        {\",    \"            var csv = line.Split(\\\",\\\");\",    \"            var data = new CustomDataType()\", " +
            "   \"            {\",    \"                Symbol = config.Symbol,\",    \"                Time = DateTime.ParseExact(csv[0], DateFormat.DB, CultureIn" +
            "fo.InvariantCulture).AddHours(20),\",    \"                Value = csv[4].ToDecimal(),\",    \"                Open = csv[1].ToDecimal(),\",    \"    " +
            "            High = csv[2].ToDecimal(),\",    \"                Low = csv[3].ToDecimal(),\",    \"                Close = csv[4].ToDecimal()\",    \"  " +
            "          };\",    \"\",    \"            return data;\",    \"        }\",    \"        catch\",    \"        {\",    \"            return null;\",  " +
            "  \"        }\",    \"    }\",    \"}\"   ]  },  {   \"cell_type\": \"code\",   \"execution_count\": null,   \"id\": \"e7608067\",   \"metadata\": {  " +
            "  \"papermill\": {     \"duration\": null,     \"end_time\": null,     \"exception\": null,     \"start_time\": null,     \"status\": \"completed\"   " +
            " },    \"tags\": [],    \"vscode\": {     \"languageId\": \"csharp\"    }   },   \"outputs\": [],   \"source\": [    \"var qb = new QuantBook();\",   " +
            " \"var symbol = qb.AddData<CustomDataType>(\\\"CustomDataType\\\", Resolution.Hour).Symbol;\",    \"\",    \"var start = new DateTime(2017, 8, 20);\"," +
            "    \"var end = start.AddHours(48);\",    \"var history = qb.History<CustomDataType>(symbol, start, end, Resolution.Hour).ToList();\",    \"\",    \"i" +
            "f (history.Count == 0)\",    \"{\",    \"    throw new Exception(\\\"No history data returned\\\");\",    \"}\"   ]  } ], \"metadata\": {  \"kernelspe" +
            "c\": {   \"display_name\": \"Foundation-C#-Default\",   \"language\": \"C#\",   \"name\": \"csharp\"  },  \"language_info\": {   \"file_extension\": \"" +
            ".cs\",   \"mimetype\": \"text/x-csharp\",   \"name\": \"C#\",   \"pygments_lexer\": \"csharp\",   \"version\": \"10.0\"  },  \"papermill\": {   \"defa" +
            "ult_parameters\": {},   \"duration\": 1.654161,   \"end_time\": \"2023-02-17T21:33:05.325357\",   \"environment_variables\": {},   \"exception\": null" +
            ",   \"input_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\RegressionTemplates\\\\BasicTemplateCustomD" +
            "ataTypeHistoryResearchCSharp.ipynb\",   \"output_path\": \"C:\\\\Users\\\\jhona\\\\QuantConnect\\\\Lean\\\\Tests\\\\bin\\\\Debug\\\\Research\\\\Regres" +
            "sionTemplates\\\\BasicTemplateCustomDataTypeHistoryResearchCSharp-output.ipynb\",   \"parameters\": {},   \"start_time\": \"2023-02-17T21:33:03.671196" +
            "\",   \"version\": \"2.4.0\"  } }, \"nbformat\": 4, \"nbformat_minor\": 5}";
    }
}
