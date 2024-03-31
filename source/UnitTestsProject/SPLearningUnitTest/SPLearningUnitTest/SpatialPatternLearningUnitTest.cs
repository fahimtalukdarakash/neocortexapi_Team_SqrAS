using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using NeoCortexApi;
//using NeoCortexApi.Entities;
using System.Net.Http.Headers;
//using Naa = NeoCortexApi.NeuralAssociationAlgorithm;
using System.Diagnostics;
namespace SPLearningUnitTest
{
    [TestClass]
    public class SpatialPatternLearningUnitTest
    {

        [TestMethod]
        [TestCategory("Testing the function for converting zeros into one at preferred index")]
        public void UnitTestConvertingZerosIntoOneAtPreferredIndex1()
        {
            SpatialPatternLearningUnitTest spl = new SpatialPatternLearningUnitTest();
            int[] arrayOfFullActiveColumns = new int[] { 0, 0, 0, 0, 0, 0 };
            int[] activeColumns = new int[] { 1, 3, 4 };
            int[] expectedFullColumns = new int[] { 0, 1, 0, 1, 1, 0 };
            arrayOfFullActiveColumns = spl.ConvertingZerosIntoOneAtPreferredIndex(arrayOfFullActiveColumns, activeColumns);
            CollectionAssert.AreEqual(expectedFullColumns, arrayOfFullActiveColumns);
        }
        [TestMethod]
        public void UnitTestConvertingZerosIntoOneAtPreferredIndex2()
        {
            SpatialPatternLearningUnitTest spl = new SpatialPatternLearningUnitTest();
            int[] arrayOfFullActiveColumns = new int[] { 0, 0, 0, 0, 0, 0 };
            int[] activeColumns = new int[] { 1, 3, 4 };
            int[] expectedFullColumns = new int[] { 0, 1, 0, 0, 1, 0 };
            arrayOfFullActiveColumns = spl.ConvertingZerosIntoOneAtPreferredIndex(arrayOfFullActiveColumns, activeColumns);
            CollectionAssert.AreNotEqual(expectedFullColumns, arrayOfFullActiveColumns);
        }
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