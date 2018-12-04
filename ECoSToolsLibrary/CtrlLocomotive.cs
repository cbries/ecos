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
	using Base;
	using ECoSCore;
	using ECoSUtils;
	using NDesk.Options;

	public class CtrlLocomotive : Base.ExecutorBase
	{
		private int _oid;
		private int _speed;
		private int _timeDelta;

		private readonly List<FncState> FncToChange = new List<FncState>();

		private void ParseArguments(string[] args)
		{
			var opts = new OptionSet();
			opts = new OptionSet {
				{ "h|help", "", v => ShowHelp(opts) },
				{ "host=", "URI of WebSocket host", v =>  { _uri = v; } },
				{ "action=", "Set the functionality of the execution.", v => _action = v },
				{ "oid=", "Object ID", v => _oid = v.ToInt(-1) },
				{ "speed=", "target speed [0..126]", v => _speed = v.ToInt() },
				{ "timeDelta=", "Milliseconds between speed changes (Minimum: 100).", v =>
					{
						_timeDelta = v.ToInt();
						if (_timeDelta < 100)
							_timeDelta = 100;
					}
				},
				{ "f=", "List of functions to change, format: NRon/off,msecdelay e.g. '0on,1000'. msecdelay is optional", v =>
					{
						int onIdx = v.IndexOf("on", StringComparison.OrdinalIgnoreCase);
						FncState state = new FncState();
						state.State = onIdx != -1;
						string vv = v.Replace("on", "").Replace("off", "");
						var parts = vv.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length > 0)
						{
							if (int.TryParse(parts[0].Trim(), out var iv))
								state.Idx = iv;
							else
								state.Idx = -1;
						}

						if (state.Idx == -1)
							return;

						if (parts.Length > 1)
						{
							if (int.TryParse(parts[1].Trim(), out var iv))
								state.Delay = iv;
						}

						FncToChange.Add(state);
					}
				}
			};
			if (args.Length == 0) ShowHelp(opts);
			opts.Parse(args);
			if (string.IsNullOrEmpty(_action)) ShowHelp(opts);
			if (string.IsNullOrEmpty(_uri))
			{
				var cfg = CtrlConfiguration.GetConfiguration("cfg.json");
				if (cfg == null)
					ShowHelp(opts);

				_uri = cfg?.Host;
			}
		}

		private void ShowHelp(OptionSet set)
		{
			Console.WriteLine("Usage: .\\CtrlLocomotive --host=ws://ip:port --action=NAME [Other Arguments]");
			set.WriteOptionDescriptions(Console.Out);
			ShowActions();
			Environment.Exit(0);
		}

		private void ShowActions()
		{
			Console.WriteLine("Actions:");
			Console.WriteLine(" stop        Stop all locomotives immediately");
			Console.WriteLine(" oids        Shows the names and object identifiers of all locomotives");
			Console.WriteLine(" info        Shows all information of a specific locomotive");
			Console.WriteLine(" velocity    Modifies the velcocity of a specific train");
			Console.WriteLine(" fncs        Activate/Deactivates functions");
			Console.WriteLine(" turn");
			Environment.Exit(0);
		}

		public CtrlLocomotive(string[] args) : base()
		{
			ParseArguments(args);
		}

		public bool Run()
		{
			var res = _ws.ConnectTo(_uri, new[] { "enableLocomotives" });
			if (!res)
			{
				Console.WriteLine($"Connection failed to {_uri} -> {_ws.LastError}");
				return false;
			}

			switch (_action)
			{
				case "stop":
					StopAllLocomotives();
					break;
				case "velocity":
					SetLocomotiveVelocity(_oid, _speed, _timeDelta);
					break;
				case "turn":
					TurnLocomotive(_oid);
					break;
				case "fncs":
					SetLocomotiveFunction(_oid, FncToChange);
					break;

				case "oids":
					ExecuteOids();
					break;
				case "info":
					ExecuteInfo();
					break;
			}

			return true;
		}

		public static void Execute(string[] args)
		{
			var instance = new CtrlLocomotive(args);
			instance.Run();
		}

		private void ExecuteOids()
		{
			var locos = GetLocomotives();
			if (locos == null)
			{
				Console.WriteLine("no locomotives available");
				return;
			}

			var locosOidSorted = locos.OrderBy(o => o.ObjectId);

			string header = string.Format("{0,-6}| {1,-25} | Locked | Info",
				" oid ", "Name");
			Console.WriteLine(header);
			for (int i = 0; i < header.Length; ++i) Console.Write("-");
			Console.WriteLine("");
			foreach (var loc in locosOidSorted)
			{
				var name = loc.Name;
				if (name.Length >= 23)
					name = name.Substring(0, 22) + "[]";

				var sb = new StringBuilder();
				sb.AppendFormat("{0,5}, ", loc.Protocol);
				sb.AppendFormat("{0,3}, ", loc.Addr);
				sb.AppendFormat("{0,3}", loc.Speed);
				sb.AppendFormat(" MAX({0,3}) BLOCK({1,3}), ",
					loc.MaxSpeedFahrstufe, loc.BlockSpeedFahrstufe);
				sb.AppendFormat("{0,3}, ", loc.GetNumberOfSpeedsteps());
				if (loc.IsForward)
					sb.Append("FW");
				else
					sb.Append("BW");

				Console.WriteLine("{0,-6}| {1,-25} |{2,-8}| {3}",
					" " + loc.ObjectId,
					name,
					loc.Locked ? " x " : " ",
					sb);
			}
		}

		private void ExecuteInfo()
		{
			var loc = GetLocomotiveByOid(_oid);
			if (loc == null)
			{
				Console.WriteLine($"locomotive with oid({_oid}) not available");
				return;
			}

			Console.WriteLine("{1,-20} {0}", loc.Name, "Name:");
			Console.WriteLine("{1,-20} {0}", loc.ObjectId, "Object ID:");
			Console.WriteLine("{1,-20} {0}", loc.Locked ? "YES" : "NO", "Locked:");
			Console.WriteLine("{1,-20} {0}", loc.Protocol, "Protocol:");
			Console.WriteLine("{1,-20} {0}", loc.Addr, "Address:");
			Console.WriteLine("{1,-20} {0}", loc.Speed, "Fahrstufe:");
			Console.WriteLine("{1,-20} {0}", loc.Direction == 1 ? "Backward" : "Forward", "Direction:");
			Console.WriteLine("{1,-20} {0}", loc.NrOfFunctions, "Nr of Functions:");

			var fncsDesc = loc.Funcdesc;
			int idx = 0;
			int newLineCounter = 0;
			foreach (var e in fncsDesc)
			{
				try
				{
					if (e.Value == 0) continue;
					var key = e.Key;
					bool state = false;
					if (idx >= 0 && idx < loc.Funcset.Count)
						state = loc.Funcset[idx];
					var fncType = e.Value;

					string typeName = "-.-";
					if (FunctionDescriptions.Functions.ContainsKey(fncType))
						typeName = FunctionDescriptions.Functions[fncType];

					string m;
					if (state)
					{
						m = $"({key,2}) {typeName,20} | ";
					}
					else
					{
						m = $" {key,2}  {typeName,20} | ";
					}

					Console.Write(m);
				}
				finally
				{
					if (e.Value != 0)
					{
						++newLineCounter;
						if (newLineCounter > 1 && newLineCounter % 3 == 0)
							Console.WriteLine("");
					}

					++idx;
				}
			}
		}
	}
}
