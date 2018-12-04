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
	using System.Linq;
	using System.Text;
	using ECoSCore;
	using Base;
	using NDesk.Options;

	public class CtrlAccessories : Base.ExecutorBase
	{
		private readonly List<int> _oids = new List<int>();
		private readonly List<int> _oidsIgnoreInInit = new List<int>();
		private int _type = -1;

		private void ParseArguments(string[] args)
		{
			var opts = new OptionSet();
			opts = new OptionSet {
				{ "h|help", "", v => ShowHelp(opts) },
				{ "host=", "URI of WebSocket host", v =>  { _uri = v; } },
				{ "action=", "Set the functionality of the execution.", v => _action = v },
				{ "oids=", "List of Object IDs of accessories to be queried.", v =>
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
				},
				{ "type=", "0:=ACCESSORRY, 1:=ROUTE", v => _type = v.ToInt(-1) }
			};
			if (args.Length == 0) ShowHelp(opts);
			try
			{
				var otherArguments = opts.Parse(args);
				if (string.IsNullOrEmpty(_action)) ShowHelp(opts);
				if (string.IsNullOrEmpty(_uri))
				{
					var cfg = CtrlConfiguration.GetConfiguration("cfg.json");
					if (cfg == null)
						ShowHelp(opts);

					foreach (var id in cfg.IgnoreOids)
						_oidsIgnoreInInit.Add(id);

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
			catch (NDesk.Options.OptionException ex)
			{
				Console.WriteLine(ex.Message);
				ShowHelp(opts);
			}
		}

		private void ShowHelp(OptionSet set)
		{
			Console.WriteLine("Usage: .\\CtrlAccessories --host=ws://ip:port --action=NAME [Other Arguments]");
			set.WriteOptionDescriptions(Console.Out);
			ShowActions();
			Environment.Exit(0);
		}

		private void ShowActions()
		{
			Console.WriteLine("Actions:");
			Console.WriteLine(" oids        Shows the names and object identifiers of all accessories");
			Console.WriteLine(" info        Shows all information of a specific accessory");
			Console.WriteLine(" switch      Changes the state of a list of accessories");
			Console.WriteLine(" init        Switches all accessories 2 times to query correct state.");
			Console.WriteLine(" listing     Listens to any accessory and shows only the changed accessory state on-demand.");
			Environment.Exit(0);
		}

		public CtrlAccessories(string[] args) : base()
		{
			ParseArguments(args);
		}

		public bool Run()
		{
			var res = _ws.ConnectTo(_uri, new[] { "enableAccessories" });
			if (!res)
			{
				Console.WriteLine($"Connection failed to {_uri} -> {_ws.LastError}");
				return false;
			}

			switch (_action)
			{
				case "switch":
					SwitchAccessories(_oids);
					break;

				case "oids":
					ExecuteOids();
					break;
				case "info":
					ExecuteInfo();
					break;
				case "init":
					ExecuteInit();
					break;
				case "listen":
					ExecuteListen();
					break;
			}

			return true;
		}

		public static void Execute(string[] args)
		{
			var instance = new CtrlAccessories(args);
			instance.Run();
		}

		private void ExecuteOids()
		{
			var accs = GetAccessories();
			if (accs == null)
			{
				Console.WriteLine("no accessories available");
				return;
			}

			var accsOidSorted = accs.OrderBy(o => o.ObjectId);

			string header = string.Format("{0,-6}| {1,-30} | Info",
				" oid ", "Name");
			Console.WriteLine(header);
			for (int i = 0; i < header.Length; ++i) Console.Write("-");
			Console.WriteLine("");
			foreach (var acc in accsOidSorted)
			{
				if (_type != -1)
				{
					if (_type == 0 && !acc.Type.Equals("ACCESSORY", StringComparison.OrdinalIgnoreCase))
						continue;
					if (_type == 1 && !acc.Type.Equals("ROUTE", StringComparison.OrdinalIgnoreCase))
						continue;
				}

				var name = acc.Name1;
				if (!string.IsNullOrEmpty(acc.Name2)) name += ", " + acc.Name2;
				if (!string.IsNullOrEmpty(acc.Name3)) name += ", " + acc.Name3;
				if (name.Length >= 28)
					name = name.Substring(0, 26) + "[]";

				var sb = new StringBuilder();
				sb.AppendFormat("{0,5} ", acc.Protocol);
				sb.AppendFormat("{0,5} ", acc.Addr);
				sb.AppendFormat("{0, 10} ", acc.Type);
				sb.AppendFormat("{0, 8} ", string.IsNullOrEmpty(acc.Mode) ? "-" : acc.Mode);
				sb.AppendFormat("{0, 2} ", acc.State);
				sb.AppendFormat("{0, 10} ", acc.InvertCommand ? "INVERTED" : "-");

				Console.WriteLine("{0,-6}| {1,-30} | {2}",
					" " + acc.ObjectId, name, sb);
			}
		}

		private void ExecuteInfo()
		{
			var n = _oids.Count;
			var accs = GetAccessories();

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

				Console.WriteLine("{1,-20} {0}", name, "Name:");
				Console.WriteLine("{1,-20} {0}", acc.ObjectId, "Object ID:");
				Console.WriteLine("{1,-20} {0}", acc.Protocol, "Protocol:");
				Console.WriteLine("{1,-20} {0}", acc.Addr, "Address:");
				Console.WriteLine("{1,-20} {0}", acc.Type, "Type");
				Console.WriteLine("{1,-20} {0}", string.IsNullOrEmpty(acc.Mode) ? "-" : acc.Mode, "Mode");
				Console.WriteLine("{1,-20} {0}", acc.State, "State");
				Console.WriteLine("{1,-20} {0}", acc.InvertCommand ? "Yes" : "No", "Inverted");

				if (i < n - 1)
					Console.WriteLine("############################################");
			}
		}

		private void ExecuteInit()
		{
			var accs = GetAccessories();
			if (accs == null)
			{
				Console.WriteLine("no accessories available");
				return;
			}

			var accsOidSorted = accs.OrderBy(o => o.ObjectId);

			foreach (var acc in accsOidSorted)
			{
				if (acc == null) continue;
				if (_oidsIgnoreInInit.Contains(acc.ObjectId)) continue;
				if (_oidsIgnoreInInit.Contains(acc.Addr)) continue;

				if (!_oids.Contains(acc.ObjectId))
					_oids.Add(acc.ObjectId);
			}

			for (int i = 0; i < 2; ++i)
			{
				var n = _oids.Count;

				for (int j = 0; j < n; ++j)
				{
					var oid = _oids[j];

					List<string> cmds;
					Console.Write($"Switch: {oid} ");
					SwitchAccessory(oid, accs, out cmds);
					if (cmds != null && cmds.Count > 0)
						_ws?.SendCommands(cmds);
					WaitForSwitchFinished(_oids);
					System.Threading.Thread.Sleep(125);
				}
			}
		}

		private void ExecuteListen()
		{
			var lastState = GetAccessories();
			foreach (var e in lastState)
			{
				if (e == null) continue;
				e.QueryState();
				_ws?.SendCommands(e);
			}

			int secondsToWait = 10;
			Console.Write($"Wait {secondsToWait} seconds");
			for (int i = 0; i < secondsToWait; ++i)
			{
				Console.Write(".");
				System.Threading.Thread.Sleep(1000);
			}
			Console.WriteLine("");
			Console.WriteLine("Start listening!");

			for (; ; )
			{
				var accs = GetAccessories();
				if (accs == null)
				{
					Console.WriteLine("no accessories available");
					break;
				}

				foreach (var a in accs)
				{
					if (a == null) continue;
					if (!a.Type.Equals("ACCESSORY")) continue;

					var oid = a.ObjectId;
					var lastStateAcc = ExecutorBase.GetAccessoryByOid(oid, lastState);
					if (lastStateAcc == null)
					{
						Console.Write("NEW ACCESSORY  ");
						Console.WriteLine($"name({a.Name1,10}) oid({a.ObjectId,5}  State(?--> {a.State})");
						lastState.Add(a);
					}
					else
					{
						if (lastStateAcc.State != a.State)
						{
							var stateName = "?";
							if (a.State == 0) stateName = "straight";
							else stateName = "turn";

							Console.Write("STATE CHANGED  ");
							Console.WriteLine($"name({a.Name1,10}) oid({a.ObjectId,5}  state({lastStateAcc.State} --> {a.State} [{stateName}])");

							var indecesToRemove = new List<int>();
							for (int idx = 0; idx < lastState.Count; ++idx)
							{
								var e = lastState[idx];
								if (e == null)
								{
									indecesToRemove.Add(idx);
									continue;
								}

								if (e.ObjectId == a.ObjectId)
								{
									indecesToRemove.Add(idx);
									continue;
								}
							}
							indecesToRemove.Reverse();
							foreach (var idx in indecesToRemove)
								lastState.RemoveAt(idx);

							lastState.Add(a);
						}
					}
				}

				System.Threading.Thread.Sleep(250);
			}
		}
	}
}
