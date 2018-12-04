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

namespace ECoSListener
{
	using System;
	using System.Collections.Generic;
	using ECoSConnector;
	using ECoSCore;
	using ECoSUtils;

	public partial class ECoSListener
    {
	    private void HandleBlocks(IReadOnlyList<IBlock> blocks)
	    {
		    foreach (var block in blocks)
		    {
			    if (block == null) continue;
			    if (string.IsNullOrEmpty(block.NativeBlock)) continue;
			    GetDataProvider().HandleData(block);
		    }
			SendCommandsOfObjects();
			Changed?.Invoke(this, GetDataProvider());
	    }

	    private void HandleBlocksS88(IReadOnlyList<IBlock> blocks)
	    {
		    foreach (var block in blocks)
		    {
			    if (block == null) continue;
			    if (string.IsNullOrEmpty(block.NativeBlock)) continue;
			    GetDataProviderS88().HandleData(block);
		    }
		    SendCommandsOfObjectsS88();
		    Changed?.Invoke(this, GetDataProviderS88());
	    }

		private void SendCommandsOfObjects()
	    {
		    var dp = GetDataProvider();
		    var objs = dp.Objects;
		    var con = GetConnector();
		    if (con == null) return;

		    lock (objs)
		    {
			    foreach (var o in objs)
			    {
				    if (o == null)
					    continue;

				    var stack = o.Commands;
				    while (stack.Count > 0)
					    con.SendCommand(stack.Pop());
			    }
		    }
	    }

	    private void SendCommandsOfObjectsS88()
	    {
		    var dp = GetDataProviderS88();
		    var objs = dp.Objects;
		    var con = GetS88Connector();
		    if (con == null) return;

		    lock (objs)
		    {
			    foreach (var o in objs)
			    {
				    if (o == null)
					    continue;

				    var stack = o.Commands;
				    while (stack.Count > 0)
					    con.SendCommand(stack.Pop());
			    }
		    }
	    }

		private void ConnectorOnFailed(object sender, MessageEventArgs ev)
        {
            Stop();
	        Log?.Error($"{ev.Message}");
        }

        private void ConnectorOnStopped(object sender, EventArgs ev)
        {
            Stop();
	        Log?.Info("ECoS connection stopped");
        }

        private void ConnectorOnStarted(object sender, EventArgs ev)
        {
			Log?.Info("ECoS connection established");

			List<ICommand> initialCommands = new List<ICommand>()
            {
                CommandFactory.Create($"request({Globals.ID_EV_BASEOBJECT}, view)"),
                CommandFactory.Create($"get({Globals.ID_EV_BASEOBJECT}, info, status)"),
                CommandFactory.Create($"request(5, view)"),
                CommandFactory.Create($"request({Globals.ID_EV_LOCOMOTIVES}, view)"),
                CommandFactory.Create($"request({Globals.ID_EV_ACCESSORIES}, view)"), // viewswitch
                CommandFactory.Create($"queryObjects({Globals.ID_EV_ACCESSORIES}, addr, protocol, type, addrext, mode, symbol, name1, name2, name3)"),
                CommandFactory.Create($"queryObjects({Globals.ID_EV_LOCOMOTIVES}, addr, name, protocol)")
            };

            Connector.SendCommands(initialCommands);
        }

	    private readonly List<string> _lines = new List<string>();

		private void ConnectorOnMessageReceived(object sender, MessageEventArgs ev)
		{
			var sw = new StopWatch();
			sw.Start();

            if (string.IsNullOrEmpty(ev.Message)) return;

	        if (BlockUtils.HasAnyBlock(ev.Message))
            {
                _lines.Clear();
                _lines.AddRange(ev.Message.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                _lines.Add(ev.Message.Trim());
            }

            if (BlockUtils.HasAnyBlock(_lines))
            {
                var blocks = BlockUtils.GetBlocks(_lines);
                HandleBlocks(blocks);
                _lines.Clear();
            }

			sw.Stop();
        }
		
	    private void S88COnFailed(object sender, MessageEventArgs ev)
	    {
		    Stop();
		    Log?.Error($"{ev.Message}");
	    }

	    private void S88COnStopped(object sender, EventArgs e)
	    {
		    Stop();
		    Log?.Info("ECoS connection for S88 stopped");
	    }

		private void S88COnStarted(object sender, EventArgs e)
	    {
		    Log?.Info("ECoS connection for S88 established");

		    List<ICommand> initialCommands = new List<ICommand>()
		    {
				CommandFactory.Create($"queryObjects({Globals.ID_EV_S88}, ports)"),
			    CommandFactory.Create($"request({Globals.ID_EV_S88}, view)")
		    };

		    S88Connector.SendCommands(initialCommands);
		}

	    private readonly List<string> _s88Lines = new List<string>();

		private void S88COnMessageReceived(object sender, MessageEventArgs ev)
		{
		    if (string.IsNullOrEmpty(ev.Message)) return;

		    if (BlockUtils.HasAnyBlock(ev.Message))
		    {
			    _s88Lines.Clear();
			    _s88Lines.AddRange(ev.Message.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries));
		    }
		    else
		    {
			    _s88Lines.Add(ev.Message.Trim());
		    }

		    if (BlockUtils.HasAnyBlock(_s88Lines))
		    {
			    var blocks = BlockUtils.GetBlocks(_s88Lines);
			    HandleBlocksS88(blocks);
			    _s88Lines.Clear();
		    }
		}
	}
}
