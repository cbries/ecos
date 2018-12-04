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
	using System.Globalization;
	using ECoSEntities;
	using Base;
	using NDesk.Options;

	public class S88Listener : Base.ExecutorBase
	{
		private RailwayEssentialContext _ctx;

		private bool _humanReadable = false;

		private void ParseArguments(string[] args)
		{
			var opts = new OptionSet();
			opts = new OptionSet {
				{ "h|help", "", v => ShowHelp(opts) },
				{ "host=", "URI of WebSocket host", v =>  { _uri = v; } },
				{ "action=", "Set the functionality of the execution.", v => _action = v },
				{ "human", "Force a human readable format.", v=> _humanReadable = true }
			};
			if (args.Length == 0) ShowHelp(opts);
			var otherArguments = opts.Parse(args);
			if (string.IsNullOrEmpty(_action)) ShowHelp(opts);
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
			Console.WriteLine("Usage: .\\S88Listener --host=ws://ip:port --action=NAME [Other Arguments]");
			set.WriteOptionDescriptions(Console.Out);
			ShowActions();
			Environment.Exit(0);
		}

		private void ShowActions()
		{
			Console.WriteLine("Actions:");
			Console.WriteLine(" show        Shows the current state of the S88 bus");
			Console.WriteLine(" listen      Listen to changes of the S88 buf");
			Environment.Exit(0);
		}

		public S88Listener(string[] args)
		{
			ParseArguments(args);

			_ctx = new RailwayEssentialContext();
		}

		public bool Run()
		{
			var res = _ws.ConnectTo(_uri, new[] { "enableS88" });
			if (!res)
			{
				Console.WriteLine($"Connection failed to {_uri} -> {_ws.LastError}");
				return false;
			}

			switch (_action)
			{
				case "listen":
					ExecuteListen();
					break;
				case "show":
					ExecuteShow();
					break;
			}

			//Console.WriteLine("Enter any key...");
			//Console.ReadKey();

			return true;
		}

		public static void Execute(string[] args)
		{
			var instance = new S88Listener(args);
			instance.Run();
		}

		private IReadOnlyList<S88> GetS88Items()
		{
			var items = new List<S88>();
			lock (_dataProviderLock)
			{
				var refToObjs = _dataProvider.Objects;
				for (int i = 0; i < refToObjs.Count; ++i)
				{
					var o = refToObjs[i] as S88;
					if (o == null) continue;
					items.Add(o);
				}
			}
			return items;
		}

		private void ShowS88Bus()
		{
			var s88Items = GetS88Items();
			int nrOfFeedback = s88Items.Count;

			var formatInfo = CultureInfo.CurrentUICulture.DateTimeFormat;
			var lastUpdate = DateTime.Now.ToString(formatInfo);

			Console.WriteLine("Number of feedback: {0}", nrOfFeedback);
			Console.WriteLine($"Last update: {lastUpdate}");

			int longestNameOfFeedback = 0;
			foreach (var it in _ctx.AvailableFeedbacks)
			{
				if (!it.IsValid()) continue;
				var n = it.Name.Length;
				if (n > longestNameOfFeedback) longestNameOfFeedback = n;
			}
			++longestNameOfFeedback;

			for (int line = 0; line < nrOfFeedback; ++line)
			{
				var s88Item = s88Items[line];
				if (s88Item == null) continue;

				if (!_humanReadable)
				{
					var len = s88Item.StateBinary.Length;
					if (len % 2 == 0)
					{
						var leftPart = s88Item.StateBinary.Substring(0, len / 2);
						var rightPart = s88Item.StateBinary.Substring(len / 2);
						Console.WriteLine($"{line,2} -> {leftPart}   {rightPart}");
					}
					else
					{
						Console.WriteLine($"{line,2} -> {s88Item.StateBinary}");
					}
				}
				else
				{
					var addr = (line + 1) * 16;

					Console.WriteLine("");
					Console.WriteLine(">> Feedback: {0}", line);

					var len = s88Item.StateBinary.Length;
					if (len % 2 == 0)
					{
						string fmt = $"{{0,{longestNameOfFeedback}}}";

						for (int i = 0; i < 16; ++i)
						{
							var localAddr = addr - i;

							var feedback = _ctx.AvailableFeedbacks.GetBy(localAddr);
							if (feedback == null)
								Console.Write(fmt, "-");
							else if (!feedback.IsValid())
								Console.Write(fmt, "!!!");
							else
								Console.Write(fmt, feedback.Name);
						}

						Console.WriteLine("");

						for (int i = 0; i < 16; ++i)
							Console.Write(string.Format(fmt, s88Item.StateBinary[i]));

						Console.WriteLine("");
					}
					else
					{
						Console.WriteLine($"{line,2} -> {s88Item.StateBinary}");
					}
				}
			}

			Console.WriteLine("");
		}

		private void ExecuteListen()
		{
			var res = WaitForDataProvider(S88.Typeid);
			if (!res)
			{
				Console.WriteLine("data provider not available");
				return;
			}

			for (; ; )
			{
				Console.Clear();

				ShowS88Bus();

				while (!_dataChanged)
					System.Threading.Thread.Sleep(125);

				_dataChanged = false;
			}
		}

		private void ExecuteShow()
		{
			var res = WaitForDataProvider(S88.Typeid);
			if (!res)
			{
				Console.WriteLine("data provider not available");
				return;
			}

			ShowS88Bus();
		}
	}
}
