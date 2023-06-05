//  Copyright (C) 2023 - Present John Roscoe Hamilton - All Rights Reserved
//  You may use, distribute and modify this code under the terms of the MIT license.
//  See the file License.txt in the root folder for full license details.

namespace WFCodeGenLib
{
    public static class CharExtensions
    {
        public static bool IsUpper(this char c)
        {
            return char.IsUpper(c);
        }
        public static bool NotUpper(this char c)
        {
            return !char.IsUpper(c);
        }
        public static bool IsDigit(this char c)
        {
            return char.IsDigit(c);
        }
        public static bool NotDigit(this char c)
        {
            return !char.IsDigit(c);
        }
        public static string AsString(this char c)
        {
            return c.ToString();
        }
        public static byte AsByte(this char c)
        {
            return (byte)c;
        }
    }
}