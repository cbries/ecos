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
using ECoSCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ECoSToolsLibrary.Base
{
	public class S88Feedback
	{
		public string Name { get; set; }
		public List<string> Description { get; set; }
		public int Address { get; set; }

		public S88Feedback()
		{
			Name = null;
			Description = new List<string>();
			Address = -1;
		}

		public bool IsValid()
		{
			if (string.IsNullOrEmpty(Name)) return false;
			if (Address <= 0) return false;
			return true;
		}

		public JObject ToJson()
		{
			var o = new JObject();
			o["name"] = Name;
			var ar = new JArray();
			foreach (var desc in Description)
			{
				if (string.IsNullOrEmpty(desc)) continue;
				ar.Add(desc);
			}
			o["description"] = ar;
			o["address"] = Address;
			return o;
		}

		public bool Parse(JToken tkn)
		{
			if (tkn == null) return false;
			try
			{
				var o = tkn as JObject;
				if (o == null) return false;
				if (o["name"] != null)
					Name = o["name"].ToString();
				if (o["address"] != null)
					Address = o["address"].ToString().ToInt(-1);
				if (o["description"] != null)
				{
					var ar = o["description"] as JArray;
					if (ar != null)
					{
						foreach (var e in ar)
						{
							if (e == null) continue;
							if (e.Type != JTokenType.String) continue;
							Description.Add(e.ToString());
						}
					}
				}
				return true;
			}
			catch(Exception ex)
			{
				Trace.WriteLine(ex.Message);
				return false;
			}
		}
	}

	public class S88FeedbackList : List<S88Feedback>
	{
		public RailwayEssentialContext Ctx { get; private set; }

		public S88FeedbackList(RailwayEssentialContext ctx)
		{
			Ctx = ctx;
		}

		public S88Feedback GetBy(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			foreach (var e in this)
			{
				if (e == null) continue;
				if (string.IsNullOrEmpty(e.Name)) continue;
				if (e.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
					return e;
			}

			return null;
		}

		public S88Feedback GetBy(int address)
		{
			if (address <= 0) return null;

			foreach (var e in this)
			{
				if (e == null) continue;
				if (e.Address == address)
					return e;
			}

			return null;
		}

		public bool Add(string name, int address, string description = null)
		{
			var item = GetBy(name);
			if (item != null) return false;
			item = new S88Feedback {Address = address, Name = name};
			if (!string.IsNullOrEmpty(description))
			{
				var lines = description.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					if (string.IsNullOrEmpty(line)) continue;
					item.Description.Add(line);
				}
			}
			base.Add(item);
			return true;
		}

		public bool Parse(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			if (!File.Exists(path)) return false;

			try
			{
				string cnt = File.ReadAllText(path, Encoding.UTF8);
				if (string.IsNullOrEmpty(cnt)) return false;

				var ar = JArray.Parse(cnt);
				if (ar == null) return false;

				foreach (var e in ar)
				{
					if (e == null) continue;
					var item = new S88Feedback();
					if (item.Parse(e))
					{
						if (item.IsValid())
							Add(item);
					}
				}

				return true;
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				return false;
			}
		}

		public bool Save(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			try
			{
				var ar = new JArray();

				foreach (var e in this)
				{
					if (e == null) continue;
					if (!e.IsValid()) continue;

					ar.Add(e.ToJson());
				}

				File.WriteAllText(path, ar.ToString(Formatting.Indented));

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
