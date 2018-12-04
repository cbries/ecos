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

using System.Collections.Generic;

namespace ECoSUtils
{
    public class FunctionDescriptions
    {
        public static Dictionary<int,string> Functions = new Dictionary<int, string>
		{
            {0002, "Function"},
            {0003, "Light"},
            {0004, "Light_0"},
            {0005, "Light_1"},
            {0007, "Sound"},
            {0008, "Music"},
            {0009, "Announce"},
            {0010, "Routing Speed"},
            {0011, "abv"},
            {0032, "Coupler"},
            {0033, "Steam"},
            {0034, "Panto"},
            {0035, "Highbeam"},
            {0036, "Bell"},
            {0037, "Horn"},
            {0038, "Whistle"},
            {0039, "Door Sound"},
            {0040, "Fan"},
            {0042, "Shovel Work Sound"},
            {0044, "Shift"},
            {0260, "Interior Lighting"},
            {0261, "Plate Light"},
            {0263, "Brakesound"},
            {0299, "Crane Raise Lower"},
            {0555, "Hook Up Down"},
            {0773, "Wheel Light"},
            {0811, "Turn"},
            {1031, "Steam Blow"},
            {1033, "Radio Sound"},
            {1287, "Coupler Sound"},
            {1543, "Track Sound"},
            {1607, "Notch up"},
            {1608, "Notch down"},
            {2055, "Thunderer Whistle"},
            {3847, "Buffer Sound"}
        };
    }
}
