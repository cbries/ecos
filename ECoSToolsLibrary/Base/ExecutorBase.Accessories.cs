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

using System;
using System.Collections.Generic;
using ECoSEntities;
using ECoSUtils;

namespace ECoSToolsLibrary.Base
{
	public partial class ExecutorBase
	{
		protected void WaitForSwitchFinished(List<int> oids)
		{
			var n = oids.Count;

			for (int i = 0; i < n; ++i)
			{
				var accs = GetAccessories();

				var oid = oids[i];
				if (oid == -1) continue;
				var acc = GetAccessoryByOid(oid, accs);
				if (acc == null) continue;

				while (acc.Switching == 1)
				{
					System.Threading.Thread.Sleep(125);
					if (_dataChanged)
					{
						accs = GetAccessories();
						acc = GetAccessoryByOid(oid, accs);
					}
				}
			}
		}

		protected List<Accessory> GetAccessories()
		{
			var res = WaitForDataProvider(Ecos2.Typeid);
			if (res == false)
				return null;

			lock (_dataProviderLock)
			{
				var accs = new List<Accessory>();
				foreach (var obj in _dataProvider.Objects)
				{
					if (!(obj is Accessory acc))
						continue;
					accs.Add(acc);
				}

				return accs;
			}
		}

		public static Accessory GetAccessoryByOid(int oid, IReadOnlyList<Accessory> knownAccessories)
		{
			if (knownAccessories == null)
			{
				Console.WriteLine("no accessories available");
				return null;
			}

			foreach (var o in knownAccessories)
			{
				if (o == null) continue;
				if (o.ObjectId == oid)
					return o;
			}

			return null;
		}

		protected void SwitchAccessory(int oid, List<Accessory> knownAccessories, out List<string> cmds)
		{
			cmds = new List<string>();
			var acc = GetAccessoryByOid(oid, knownAccessories);
			if (acc == null)
			{
				Console.WriteLine($"accessory with oid({oid}) not available");
				return;
			}

			if (string.IsNullOrEmpty(acc.Type)) return;
			if (acc.Type.Equals("ROUTE", StringComparison.OrdinalIgnoreCase)) return;
			if (!acc.Type.Equals("ACCESSORY", StringComparison.OrdinalIgnoreCase)) return;

			var currentState = acc.State;
			var targetState = acc.State;
			if (targetState == 1) targetState = 0;
			else if (targetState == 0) targetState = 1;
			Console.WriteLine($" {currentState} -> {targetState} ## ");
			acc.Switch(targetState);
			Console.WriteLine($"{acc.Caption}");
			while (acc.Commands.Count != 0)
			{
				var c = acc.Commands.Pop() as ICommand;
				if (c == null) continue;
				cmds.Add(c.NativeCommand);
			}
		}

		protected void SwitchAccessories(List<int> oids)
		{
			WaitForSwitchFinished(oids);

			List<string> cmds = null;
			var n = oids.Count;
			var accs = GetAccessories();
			for (int i = 0; i < n; ++i)
			{
				var oid = oids[i];
				if (oid == -1) continue;
				SwitchAccessory(oid, accs, out cmds);
			}

			if (cmds != null && cmds.Count > 0)
				_ws?.SendCommands(cmds);

			WaitForSwitchFinished(oids);
		}
	}
}
