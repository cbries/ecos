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
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ECoSToolsLibrary.Base
{
	public class CtrlConfiguration
	{
		public string Host { get; set; }
		public List<int> IgnoreOids { get; set; }
		public string Path { get; private set; }
		public string LastError { get; private set; }

		private static CtrlConfiguration _instance;

		public CtrlConfiguration()
		{
			IgnoreOids = new List<int>();
		}

		public static CtrlConfiguration GetConfiguration(string path)
		{
			if (_instance != null)
				return _instance;
			_instance = new CtrlConfiguration();
			if (!_instance.Parse(path))
				_instance = null;
			return _instance;
		}

		public bool Save()
		{
			try
			{
				var obj = new JObject();
				obj["host"] = Host;
				File.WriteAllText(Path, obj.ToString(Formatting.Indented));
				return true;
			}
			catch (Exception ex)
			{
				LastError = ex.Message;
				return false;
			}
		}

		public bool Parse(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			Path = path;
			if (!File.Exists(path)) return false;
			try
			{
				string cnt = File.ReadAllText(path, Encoding.UTF8);
				if (string.IsNullOrEmpty(cnt)) return false;
				var obj = JObject.Parse(cnt);
				if (obj["host"] != null)
					Host = obj["host"].ToString();

				if (obj["ignoreInit"] != null)
				{
					var ar = obj["ignoreInit"] as JArray;
					if (ar != null)
					{
						for (int i = 0; i < ar.Count; ++i)
						{
							var o = ar[i] as JObject;
							if (o == null) continue;
							var oids = o["oids"] as JArray;
							if (oids == null) continue;
							foreach (var id in oids)
							{
								if (!IgnoreOids.Contains((int)id))
									IgnoreOids.Add((int)id);
							}
						}
					}
				}
				return true;
			}
			catch(Exception ex)
			{
				LastError = ex.Message;
				return false;
			}
		}
	}
}
