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

namespace ECoSPendelzug
{
	using System;
	using System.Threading;
	using System.Collections.Generic;
	using ECoSEntities;

	class Program
	{
		static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);

		static void Main(string[] args)
		{
			// Lokomotive: 74 854 DB
			int locToTrigger = 1004;
			int maxSpeed = 40;

			int s88id = 102; // S88 module
			uint s88pinRight = 16; // S88 module pin: FB3
			uint s88pinLeft = 13; // S88 module pin: FBL4

			int waitDelay = 5 * 1000;

			Locomotive locItem = null;
			S88 s88Item = null;

			bool isStarted = false;
			bool isDrivingBackward = true;
			List<string> cmdsToSend = null;

			var ws = new WsConnect();
			ws.WsData += (sender, dp) =>
			{
				if (dp.GetObjectBy(s88id) is S88 localS88Item)
					s88Item = localS88Item;
				if (dp.GetObjectBy(locToTrigger) is Locomotive localLocItem)
					locItem = localLocItem;

				if (s88Item == null) return;
				if (locItem == null) return;

				var stateRight = (s88Item?.Pin(s88pinRight)).Value;
				var stateLeft = (s88Item?.Pin(s88pinLeft)).Value;

				if (!isStarted)
				{
					// drive from right -> left, i.e. backward
					if (stateRight && !stateLeft) isDrivingBackward = true;
					// drive from left -> right, i.e. forward
					if (stateLeft && !stateRight) isDrivingBackward = false;

					locItem.ChangeDirection(isDrivingBackward);
					locItem.ChangeSpeed(maxSpeed, true, true);

					cmdsToSend = locItem.GetCommands() as List<string>;
					ws.SendCommands(cmdsToSend);
					cmdsToSend?.Clear();

					Console.WriteLine("Started!");

					isStarted = true;
				}
				else
				{
					if (isDrivingBackward)
					{
						if (stateLeft)
						{
							Console.WriteLine("Stop left!");

							locItem.Stop();
							locItem.ChangeDirection(false);
							isDrivingBackward = false;
							cmdsToSend = locItem.GetCommands() as List<string>;
							ws.SendCommands(cmdsToSend);
							cmdsToSend?.Clear();
							Console.WriteLine($"Wait {waitDelay} msecs!");
							Thread.Sleep(waitDelay);

							Console.WriteLine("Start to right!");
							locItem.ChangeSpeed(maxSpeed, true, true);
							cmdsToSend = locItem.GetCommands() as List<string>;
							ws.SendCommands(cmdsToSend);
							cmdsToSend?.Clear();
						}
					}
					else
					{
						if (stateRight)
						{
							Console.WriteLine("Stop right!");

							locItem.Stop();
							locItem.ChangeDirection(true);
							isDrivingBackward = true;
							cmdsToSend = locItem.GetCommands() as List<string>;
							ws.SendCommands(cmdsToSend);
							cmdsToSend?.Clear();
							Console.WriteLine($"Wait {waitDelay} msecs!");
							Thread.Sleep(waitDelay);

							Console.WriteLine("Start to left!");
							locItem.ChangeSpeed(maxSpeed, true, true);
							cmdsToSend = locItem.GetCommands() as List<string>;
							ws.SendCommands(cmdsToSend);
							cmdsToSend?.Clear();
						}
					}
				}
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
