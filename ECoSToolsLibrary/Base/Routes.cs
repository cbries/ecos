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
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ECoSToolsLibrary.Base
{
	public class RouteAccessory
	{
		public string Name { get; set; }
		public string State { get; set; }

		public bool IsValid()
		{
			if (string.IsNullOrEmpty(Name)) return false;
			if (string.IsNullOrEmpty(State)) return false;
			return true;
		}

		public bool Parse(JToken tkn)
		{
			var o = tkn as JObject;
			if (o == null) return false;
			if (o["name"] != null)
				Name = o["name"].ToString();
			if (o["state"] != null)
				State = o["state"].ToString();
			return true;
		}

		public JObject ToJson()
		{
			var o = new JObject
			{
				["name"] = Name,
				["state"] = State
			};
			return o;
		}
	}

	public class Route
	{
		public RailwayEssentialContext Ctx { get; private set; }

		public string GroupName { get; set; }

		public Block SourceBlock { get; set; }
		public Block TargetBlock { get; set; }

		public List<RouteAccessory> Accessories { get; set; }

		public Route(RailwayEssentialContext ctx)
		{
			Ctx = ctx;		
		}

		public bool IsValid()
		{
			if (SourceBlock == null) return false;
			if (TargetBlock == null) return false;
			
			return true;
		}

		public bool Parse(JToken tkn)
		{
			GroupName = string.Empty;
			SourceBlock = null;
			TargetBlock = null;
			Accessories = new List<RouteAccessory>();

			var o = tkn as JObject;
			if (o == null) return false;
			if (Ctx == null) return false;

			try
			{
				if (o["groupName"] != null)
					GroupName = o["groupName"].ToString();

				string srcBlk = null;
				string dstBlk = null;

				if (o["sourceBlock"] != null)
					srcBlk = o["sourceBlock"].ToString();
				if (o["targetBlock"] != null)
					dstBlk = o["targetBlock"].ToString();

				if (Ctx != null && Ctx.AvailableBlocks.Count > 0)
				{
					if (!string.IsNullOrEmpty(srcBlk))
						SourceBlock = Ctx.AvailableBlocks.GetBy(srcBlk.Trim());
					if (!string.IsNullOrEmpty(dstBlk))
						TargetBlock = Ctx.AvailableBlocks.GetBy(dstBlk.Trim());
				}

				var accs = o["accessories"] as JArray;
				if (accs != null)
				{
					foreach (var a in accs)
					{
						if (a == null) continue;
						var blkAcc = new RouteAccessory();
						if (blkAcc.Parse(a))
							Accessories.Add(blkAcc);
					}
				}

				return true;
			}
			catch
			{
				return false;
			}
		}

		public JObject GetJson()
		{
			var o = new JObject();
			o["groupName"] = GroupName;

			if (SourceBlock != null && !string.IsNullOrEmpty(SourceBlock.Name))
				o["sourceBlock"] = SourceBlock.Name;
			else
				o["sourceBlock"] = null;

			if (TargetBlock != null && !string.IsNullOrEmpty(TargetBlock.Name))
				o["targetBlock"] = TargetBlock.Name;
			else
				o["targetBlock"] = null;

			var ar = new JArray();
			for (int i = 0; i < Accessories.Count; ++i)
			{
				var acc = Accessories[i];
				if (acc == null) continue;
				if (!acc.IsValid()) continue;
				ar.Add(acc.ToJson());
			}
			o["accessories"] = ar;

			return o;
		}
	}

	public class Routes : List<Route>
	{
		public RailwayEssentialContext Ctx { get; private set; }

		public Routes(RailwayEssentialContext ctx)
		{
			Ctx = ctx;
		}

		public bool Parse(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			if (!File.Exists(path)) return false;

			try
			{
				var cnt = File.ReadAllText(path, Encoding.UTF8);
				if (string.IsNullOrEmpty(cnt)) return false;
				var ar = JArray.Parse(cnt);
				if (ar == null) return false;

				for (int i = 0; i < ar.Count; ++i)
				{
					var item = ar[i];
					if (item == null) continue;
					var b = new Route(Ctx);
					if (b.Parse(item))
					{
						if (b.IsValid())
							base.Add(b);
					}
				}
			}
			catch(Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}

			return true;
		}

		public bool Save(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			try
			{
				var ar = new JArray();
				foreach (var item in this)
				{
					if (item == null) continue;
					ar.Add(item.GetJson());
				}
				File.WriteAllText(path, ar.ToString(Formatting.Indented), Encoding.UTF8);
				return true;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				return false;
			}
		}
	}
}
