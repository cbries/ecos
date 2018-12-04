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
using System.Linq;
using ECoSCore;
using ECoSEntities;
using ECoSUtils;

namespace ECoSToolsLibrary.Base
{
	public partial class ExecutorBase
	{
		protected List<Locomotive> GetLocomotives()
		{
			var res = WaitForDataProvider(Ecos2.Typeid);
			if (res == false)
				return null;

			lock (_dataProviderLock)
			{
				var locos = new List<Locomotive>();
				foreach (var obj in _dataProvider.Objects)
				{
					if (!(obj is Locomotive loc))
						continue;

					locos.Add(loc);
				}

				return locos;
			}
		}

		protected Locomotive GetLocomotiveByOid(int oid)
		{
			var locos = GetLocomotives();
			if (locos == null)
			{
				Console.WriteLine("no locomotives available");
				return null;
			}

			foreach (var o in locos)
			{
				if (o == null) continue;
				if (o.ObjectId == oid)
					return o;
			}

			return null;
		}

		protected void StopAllLocomotives()
		{
			var locos = GetLocomotives();
			if (locos == null)
			{
				Console.WriteLine("no locomotives available");
				return;
			}

			var cmds = new List<string>();
			foreach (var loc in locos)
			{
				if (loc == null) continue;
				loc.ChangeSpeed(0);
				while (loc.Commands.Count != 0)
				{
					var c = loc.Commands.Pop() as ICommand;
					if (c == null) continue;
					cmds.Add(c.NativeCommand);
				}
			}

			cmds.Add("set(1, stop)"); // stop ECoS

			_ws?.SendCommands(cmds);
		}

		protected void SetLocomotiveVelocity(int oid, int targetSpeed, int timeDelta)
		{
			var loc = WaitForLoc(oid);
			if (loc == null)
			{
				Console.WriteLine("Locomotive does not exist");
				return;
			}

			Console.WriteLine($" > Current speed: {loc.Speed}");
			Console.WriteLine($" > Target speed:  {targetSpeed}");
			Console.WriteLine($" > Delta [msec] between updates: {timeDelta}");

			if (targetSpeed <= 0) targetSpeed = 0;
			if (targetSpeed >= 126) targetSpeed = 126;

			bool increase = loc.Speed < targetSpeed;

			if (timeDelta == 0)
			{
				loc.ChangeSpeed(targetSpeed, true, true);
				_ws?.SendCommands(loc);
				Console.WriteLine($"Final Speed: {targetSpeed}");
			}
			else
			{
				var currentSpeed = loc.Speed;
				var localTargetSpeed = targetSpeed;

				int deltaSpeed;
				if (localTargetSpeed > currentSpeed) deltaSpeed = localTargetSpeed - currentSpeed;
				else deltaSpeed = currentSpeed - localTargetSpeed;

				var localWaitMsecs = timeDelta;
				var localStepIncrement = 1;

				if (deltaSpeed <= 10)
				{
				}
				else if (deltaSpeed <= 25)
				{
					localWaitMsecs = 2 * timeDelta;
					localStepIncrement = 2;
				}
				else if (deltaSpeed <= 50)
				{
					localWaitMsecs = 4 * timeDelta;
					localStepIncrement = 4;
				}
				else if (deltaSpeed <= 75)
				{
					localWaitMsecs = 10 * timeDelta;
					localStepIncrement = 10;
				}
				else if (deltaSpeed <= 100)
				{
					localWaitMsecs = 15 * timeDelta;
					localStepIncrement = 15;
				}
				else if (deltaSpeed <= 125)
				{
					localWaitMsecs = 20 * timeDelta;
					localStepIncrement = 20;
				}

				for (int step = 0; step < deltaSpeed; step += localStepIncrement)
				{
					float percent = (float)step / (float)deltaSpeed * 100.0f;
					int stepSpeed;
					if (increase)
						stepSpeed = currentSpeed + step;
					else
						stepSpeed = currentSpeed - step;

					loc.ChangeSpeed(stepSpeed, step == 0);
					_ws?.SendCommands(loc);

					Console.WriteLine($" # {percent}% - current speed: {stepSpeed}");

					System.Threading.Thread.Sleep(localWaitMsecs);

					if (stepSpeed == localTargetSpeed)
						break;
				}

				loc.ChangeSpeed(localTargetSpeed, false, true);
				_ws?.SendCommands(loc);
				Console.WriteLine($"Final Speed: {localTargetSpeed}");
			}
		}

		protected void TurnLocomotive(int oid)
		{
			var loc = GetLocomotiveByOid(oid);
			if (loc == null)
			{
				Console.WriteLine($"locomotive with oid({oid}) not available");
				return;
			}

			if (loc.IsBackward) loc.ChangeDirection(false);
			else if (loc.IsForward) loc.ChangeDirection(true);

			loc.QueryState();

			_ws?.SendCommands(loc);
		}

		protected void SetLocomotiveFunction(int oid, List<FncState> fncToChange)
		{
			var loc = GetLocomotiveByOid(oid);
			if (loc == null)
			{
				Console.WriteLine($"locomotive with oid({oid}) not available");
				return;
			}

			var fncsWithDelay = new List<FncState>();

			var states = new Dictionary<uint, bool>();
			foreach (var fncState in fncToChange)
			{
				if (fncState == null) continue;
				if (fncState.Delay > 0)
					fncsWithDelay.Add(fncState);

				var key = (uint)fncState.Idx;
				var value = fncState.State;

				if (!states.ContainsKey(key))
					states.Add(key, value);
			}

			loc.ToggleFunctions(states);
			_ws?.SendCommands(loc);

			if (fncsWithDelay.Any())
			{
				states.Clear();

				var sw = new StopWatch();
				sw.Start();

				for (int i = 0; i < maxWaitFncs; i += sleepMsecs)
				{
					List<int> indecesToRemove = new List<int>();

					var n = fncsWithDelay.Count;

					if (n == 0) break;

					for (int j = 0; j < n; ++j)
					{
						var fncState = fncsWithDelay[j];
						if (fncState == null) continue;
						var elapsed = sw.ElapsedMilliseconds;

						if (elapsed > fncState.Delay)
						{
							indecesToRemove.Add(j);

							var key = (uint)fncState.Idx;
							var value = !fncState.State;

							if (!states.ContainsKey(key))
								states.Add(key, value);
						}

						indecesToRemove.Reverse();

						foreach (var idx in indecesToRemove)
							fncsWithDelay.RemoveAt(idx);
					}

					if (states.Count > 0)
					{
						loc.ToggleFunctions(states);
						_ws?.SendCommands(loc);
					}

					System.Threading.Thread.Sleep(sleepMsecs);
				}
			}
		}

		private Locomotive WaitForLoc(int oid)
		{
			for (int i = 0; i < maxWait; i += sleepMsecs)
			{
				if (!_dataChanged || _dataProvider == null)
					goto WaitToNext;

				lock (_dataProviderLock)
				{
					var o = _dataProvider.GetObjectBy(oid);
					if (o == null) goto WaitToNext;

					if (o.TypeId() != Locomotive.Typeid)
					{
						Console.WriteLine("Error: object id does not address Locomotive");
						return null; // if type ids do not match we have a wrong object id
					}

					var loco = o as Locomotive;
					if (loco == null) goto WaitToNext;

					return loco;
				}

				WaitToNext:
				System.Threading.Thread.Sleep(sleepMsecs);
			}

			return null;
		}		
	}
}
