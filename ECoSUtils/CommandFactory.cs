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
using System;

namespace ECoSUtils
{
    public enum CommandT
    {
        Unknown = -1, Get, Set, Create, Request, QueryObjects, Release
    }

    public static class CommandFactory
    {
        public static ICommand Create(string cmdline, bool keepQuotes=false)
        {
            if (string.IsNullOrEmpty(cmdline)) return null;
	        var cmdEnd = cmdline.IndexOf("(", StringComparison.OrdinalIgnoreCase);
            if (cmdEnd == -1) return null;
            var cmdName = cmdline.Substring(0, cmdEnd);
            if (cmdName.Length < 3) return null;

			var cmp = StringComparison.OrdinalIgnoreCase;
			ICommand cmd;
            if(cmdName.Equals(Commands.Get.N, cmp))
                cmd = new Commands.Get();
            else if(cmdName.Equals(Commands.Set.N, cmp))
                cmd = new Commands.Set();
            else if(cmdName.Equals(Commands.Create.N, cmp))
                cmd = new Commands.Create();
            else if(cmdName.Equals(Commands.QueryObjects.N, cmp))
                cmd = new Commands.QueryObjects();
            else if(cmdName.Equals(Commands.Release.N, cmp))
                cmd = new Commands.Release();
            else if (cmdName.Equals(Commands.Request.N, cmp))
                cmd = new Commands.Request();
            else
                cmd = new Commands.Unknown();

            cmd.NativeCommand = cmdline;
            cmd.Parse(keepQuotes);

            return cmd;
        }
    }
}
