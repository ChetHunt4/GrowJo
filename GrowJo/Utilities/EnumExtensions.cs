using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrowJo.Utilities
{
    public static class EnumExtensions
    {
        public static T Next<T>(this T src) where T : Enum
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("Argument is not an enum");
            }
            Array arr = Enum.GetValues(src.GetType());
            int currentIndex = Array.IndexOf(arr, src);
            int nextIndex = (currentIndex + 1) % arr.Length;
            return (T)arr.GetValue(nextIndex)!;

        }
    }
}
