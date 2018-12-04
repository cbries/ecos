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

namespace ECoSS88Actions
{
	using System;
	using System.Threading;
	using System.Collections.Generic;
	using ECoSEntities;

	class Program
	{
		static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);

		static void Main()
		{
			// Lokomotive: 18 527 DRG
			int locToTrigger = 1000;
			uint objectFuncIdx = 2;

			// S88 Feedback: FB3
			int s88id = 102;
			uint s88pin = 16;

			Locomotive locItem = null;
			S88 s88Item = null;

			var ws = new WsConnect();
			ws.WsData += (sender, dp) =>
			{
				if (dp.GetObjectBy(s88id) is S88 localS88Item)
					s88Item = localS88Item;
				if (dp.GetObjectBy(locToTrigger) is Locomotive localLocItem)
					locItem = localLocItem;

				if (s88Item == null || locItem == null) return;
				var currentState = s88Item?.Pin(s88pin);
				var currentPinState = currentState.GetValueOrDefault(false);
				locItem.ToggleFunctions(new Dictionary<uint, bool> {
					{objectFuncIdx, currentPinState}
				});

				Console.WriteLine($"Name: {locItem.Name}, State: {currentPinState}");

				var cmdsToSend = locItem.GetCommands() as List<string>;
				ws.SendCommands(cmdsToSend);
			};

			var res = ws.ConnectTo("ws://127.0.0.1:10050", new[] {
					"enableS88", "enableLocomotives"
				});

			if (res) Console.WriteLine("Wait for data...");

			Console.CancelKeyPress += (sender, eArgs) =>
			{
				QuitEvent.Set();
				eArgs.Cancel = true;
			};

			QuitEvent.WaitOne();
		}
	}
}
