/*
 * MIT License
 *
 * Copyright (c) 2018 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace ECoSToolsLibrary
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Base;
	using ECoSCore;
	using ECoSEntities;
	using NDesk.Options;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	public class Teaching : ExecutorBase
	{
		private RailwayEssentialContext _ctx;

		private string _targetRouteName;
		private readonly List<int> _oids = new List<int>();

		private void ParseArguments(string[] args)
		{
			var opts = new OptionSet();
			opts = new OptionSet {
				{ "h|help", "", v => ShowHelp(opts) },
				{ "host=", "URI of WebSocket host", v =>  { _uri = v; } },
				{ "route=", "Name of the target route.", v => _targetRouteName = v },
				{ "oids=", "List of Object ID for which the state is queried.", v =>
					{
						var parts = v.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
						foreach (var p in parts)
						{
							if (string.IsNullOrEmpty(p)) continue;
							var iv = p.ToInt(-1);
							if (iv == -1) continue;
							if(!_oids.Contains(iv))
								_oids.Add(iv);
						}
					}
				}
			};
			if (args.Length == 0) ShowHelp(opts);
			var otherArguments = opts.Parse(args);
			if (string.IsNullOrEmpty(_targetRouteName)) ShowHelp(opts);
			if (_oids.Count <= 0) ShowHelp(opts);
			if (string.IsNullOrEmpty(_uri))
			{
				var cfg = CtrlConfiguration.GetConfiguration("cfg.json");
				if (cfg == null)
					ShowHelp(opts);

				_uri = cfg?.Host;
			}

			if (otherArguments.Count > 0)
			{
				Console.WriteLine("UNKNWON");
				foreach (var arg in otherArguments)
					Console.WriteLine($"  ?? {arg}");
				Console.WriteLine("");
				ShowHelp(opts);
			}
		}

		private void ShowHelp(OptionSet set)
		{
			Console.WriteLine("Usage: .\\Teaching --host=ws://ip:port --action=NAME [Other Arguments]");
			set.WriteOptionDescriptions(Console.Out);
			Environment.Exit(0);
		}

		public Teaching(string[] args)
		{
			ParseArguments(args);

			_ctx = new RailwayEssentialContext();
		}

		public bool Run()
		{
			var res = _ws.ConnectTo(_uri, new[] { "enableAccessories" });
			if (!res)
			{
				Console.WriteLine($"Connection failed to {_uri} -> {_ws.LastError}");
				return false;
			}

			ExecuteTeaching();

			//Console.WriteLine("Enter any key...");
			//Console.ReadKey();

			return true;
		}

		public static void Execute(string[] args)
		{
			var instance = new Teaching(args);
			instance.Run();
		}

		private void ExecuteTeaching()
		{
			var res = WaitForDataProvider(Accessory.Typeid);
			if (!res)
			{
				Console.WriteLine("data provider not available");
				return;
			}

			var n = _oids.Count;
			var accs = GetAccessories();

			var resultObj = new JObject();
			resultObj["route"] = _targetRouteName;
			var accessoryStates = new JArray();
			resultObj["accessories"] = accessoryStates;

			for (int i = 0; i < n; ++i)
			{
				var oid = _oids[i];
				if (oid == -1) continue;
				var acc = GetAccessoryByOid(oid, accs);
				if (acc == null)
				{
					Console.WriteLine($"accessory with oid({oid}) not available");
					return;
				}

				var name = acc.Name1;
				if (!string.IsNullOrEmpty(acc.Name2)) name += ", " + acc.Name2;
				if (!string.IsNullOrEmpty(acc.Name3)) name += ", " + acc.Name3;

				var accessoryObj = new JObject
				{
					["name"] = name,
					["objectId"] = acc.ObjectId,
					["protocol"] = acc.Protocol,
					["addr"] = acc.Addr,
					["state"] = acc.State,
					["type"] = acc.Type,
					["mode"] = acc.Mode,
					["inverted"] = acc.InvertCommand
				};

				accessoryStates.Add(accessoryObj);
			}

			var targetFilePath = $"{_targetRouteName}.json";
			try
			{
				if (File.Exists(targetFilePath))
					File.Delete(targetFilePath);
				File.WriteAllText(targetFilePath, resultObj.ToString(Formatting.Indented));
			}
			catch (Exception ex)
			{
				Console.WriteLine("<Error> {0}", ex.Message);
			}
		}
	}
}
