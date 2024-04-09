using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeoCortexApiSample
{
    public class SPLHelpers
    {
        ///<summary>
        ///above activeColumns gives only the columns number
        ///<param name="arrayOfFullActiveColumns">
        ///This arrayOfFullActiveColumns will have full 1024 columns and in that which columns are active that column will be 1 and rest of will be zero
        ///</param>
        ///</summary>
        public int[] ConvertingZerosIntoOneAtPreferredIndex(int[] arrayOfFullActiveColumns, int[] activeColumns)
        {
            int j = 0;
            for (int i = 0; i < arrayOfFullActiveColumns.Length; i++)
            {
                if (activeColumns.Length == 0)
                {
                    continue;
                }
                else
                {
                    if (i == activeColumns[j])
                    {
                        arrayOfFullActiveColumns[i] = 1;
                        if (j == activeColumns.Length - 1) break;
                        j++;
                    }
                }
            }
            return arrayOfFullActiveColumns;
        }

    }
}
