/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
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

using System.Collections.Generic;
using System.Text;
using ECoSCore;
using ECoSUtils;
using Newtonsoft.Json.Linq;

namespace ECoSEntities
{
    public abstract class Item
        : IItem
        , IItemView
        , IItemSerializer
		, ICommandsBase64
	{
        protected Item()
        {
            ObjectId = -1;
        }

        private readonly Stack<object> _commands = new Stack<object>();

        protected void AddCmd(ICommand cmd)
        {
            lock (_commands)
            {
                _commands?.Push(cmd);
            }
        }

        #region IItem

        public int ObjectId { get; set; }

        public Stack<object> Commands
        {
            get
            {
                lock (_commands)
                {
                    return _commands;
                }
            }
        }

		public IReadOnlyList<string> GetCommands()
		{
			var cmds = new List<string>();
			while (Commands.Count != 0)
			{
				var c = Commands.Pop() as ICommand;
				if (c == null) continue;
				cmds.Add(c.NativeCommand);
			}

			return cmds;
		}

		public virtual string Caption => "-.-";

        public virtual int TypeId()
        {
            return -1;
        }

        public virtual bool Parse(List<object> arguments)
        {
            return false;
        }

        public virtual void QueryState()
        {

        }

        #endregion

        #region IItemView

        public bool HasView { get; private set; }

        public void EnableView()
        {
            if (HasView) return;
            AddCmd(CommandFactory.Create($"request({ObjectId}, view)"));
            HasView = true;
        }

        public void DisableView()
        {
            if (!HasView) return;
            AddCmd(CommandFactory.Create($"release({ObjectId}, view)"));
            HasView = false;
        }

        #endregion

        #region IItemSerializer

        public virtual JObject ToJson()
        {
            return null;
        }

        public virtual void ParseJson(JObject obj)
        {

        }

		#endregion

		#region ICommandsBase64

		public virtual string ToBase64()
		{
			if (Commands == null || Commands.Count == 0)
				return null;

			var cmdArray = Commands.ToArray();

			var sb = new StringBuilder();
			for (int i = 0; i < cmdArray.Length; ++i)
			{
				var cmd = cmdArray[i] as ICommand;
				if(cmd == null) continue;
				sb.Append(cmd.NativeCommand);
				sb.Append("\n");
			}

			return Encoding.UTF8.ToBase64(sb.ToString());
		}

		#endregion

		public static Stack<object> GetCommands(string encodedCommands)
		{
			if (string.IsNullOrEmpty(encodedCommands)) return null;
			if (Encoding.UTF8.TryParseBase64(encodedCommands, out var decodedText))
			{
				try
				{
					var cmds = new Stack<object>();
					var lines = decodedText.Split('\n');
					foreach (var line in lines)
					{
						if (string.IsNullOrEmpty(line)) continue;
						var cmd = CommandFactory.Create(line);
						cmds.Push(cmd);
					}

					return cmds;
				}
				catch
				{
					// ignore
				}
			}
			return null;
		}
	}
}
