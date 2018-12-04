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

using System.Linq;
using ECoSEntities;

namespace ECoSToolsLibrary.Base
{
	public partial class ExecutorBase
	{
		protected IDataProvider _dataProvider = new DataProvider(DataProvider.DataModeT.Any);
		protected object _dataProviderLock = new object();
		protected bool _dataChanged;

		private bool HasAny(IDataProvider dp, int typeId)
		{
			if (dp == null) return false;
			return _dataProvider.Objects.Any(o => o.TypeId() == typeId);
		}

		protected bool WaitForDataProvider(int typeId)
		{
			for (int i = 0; i < maxWait; i += sleepMsecs)
			{
				if (!_dataChanged || _dataProvider == null)
					goto WaitToNext;

				lock (_dataProviderLock)
				{
					if (_dataProvider != null)
					{
						if (!HasAny(_dataProvider, typeId))
							continue;
					}

					if (_dataProvider != null)
					{
						_dataChanged = false;

						return true;
					}
				}

				WaitToNext:
				if (_dataProvider != null && HasAny(_dataProvider, typeId))
					return true;
				System.Threading.Thread.Sleep(sleepMsecs);
			}

			return false;
		}

	}
}
