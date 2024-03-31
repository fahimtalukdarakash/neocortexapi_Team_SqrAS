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
        [TestMethod]
        [TestCategory("Testing the function for all inputs has SDR or not")]
        public void AllInputsHaveSDRArrays_ReturnsTrue()
        {
            SpatialPatternLearningUnitTest spl = new SpatialPatternLearningUnitTest();
            // Arrange
            var inputofSDRspercycle = new Dictionary<double, List<int[]>>()
            {
                { 1.0, new List<int[]>() { new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 } } },
                { 2.0, new List<int[]>() { new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 } } },
            // Add more entries as needed
            };
            int lengthoftotalinputs = 2;
            bool SDRofallinputs = false;

            // Act
            var result = spl.CheckingOfAllInputHaveSDRorNot(inputofSDRspercycle, lengthoftotalinputs, SDRofallinputs);

            // Assert
            Assert.IsTrue(result);
        }
        [TestMethod]
        public void NoInputsHaveSDRArrays_ReturnsFalse()
        {
            SpatialPatternLearningUnitTest spl = new SpatialPatternLearningUnitTest();
            // Arrange
            var inputofSDRspercycle = new Dictionary<double, List<int[]>>()
            {
                { 1.0, new List<int[]>() },
                { 2.0, new List<int[]>() },
            // Add more entries as needed
            };
            int lengthoftotalinputs = 2;
            bool SDRofallinputs = false;

            // Act
            var result = spl.CheckingOfAllInputHaveSDRorNot(inputofSDRspercycle, lengthoftotalinputs, SDRofallinputs);

            // Assert
            Assert.IsFalse(result);
        }
        public bool CheckingOfAllInputHaveSDRorNot(Dictionary<double, List<int[]>> inputofSDRspercycle, int lengthoftotalinputs, bool SDRofallinputs)
        {
            Dictionary<double, int> a = new Dictionary<double, int>();
            foreach (var input in inputofSDRspercycle)
            {
                if (SDRofallinputs == false)
                {
                    double i = input.Key;
                    List<int[]> values = input.Value;
                    foreach (var SDRarray in values)
                    {
                        if (SDRarray.Length == 0)
                        {
                            a[i] = 0;
                        }
                        else
                        {
                            a[i] = 1;
                        }
                    }
                }
                else
                {
                    break;
                }

            }
            int count = 0;
            foreach (var b in a)
            {
                if (b.Value == 1)
                {
                    count++;
                }
            }
            if (count == lengthoftotalinputs)
            {
                SDRofallinputs = true;
            }
            return SDRofallinputs;
        }
        [TestMethod]
        [TestCategory("Testing the function for comparing the last two cycles SDR of an input")]
        public void AllInputsHaveEqualArraysForLastTwoCycles_ReturnsTrue()
        {
            // Arrange
            var inputofSDRspercycle = new Dictionary<double, List<int[]>>()
            {
                { 1.0, new List<int[]>() { new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 } } },
                { 2.0, new List<int[]>() { new int[] { 4, 5, 6 }, new int[] { 4, 5, 6 } } },
            // Add more entries as needed
            };
            bool c = false;

            // Act
            var result = ComparingOfSDRsForEachCyclePerInput(inputofSDRspercycle, c);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void SomeInputsHaveDifferentArraysForLastTwoCycles_ReturnsFalse()
        {
            // Arrange
            var inputofSDRspercycle = new Dictionary<double, List<int[]>>()
            {
                { 1.0, new List<int[]>() { new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 } } },
                { 2.0, new List<int[]>() { new int[] { 4, 5, 6 }, new int[] { 4, 6, 5 } } },
            // Add more entries as needed
            };
            bool c = false;

            // Act
            var result = ComparingOfSDRsForEachCyclePerInput(inputofSDRspercycle, c);

            // Assert
            Assert.IsFalse(result);
        }
        [TestMethod]
        public void SomeInputsHaveDifferentLengthArraysForLastTwoCycles_ReturnsFalse()
        {
            // Arrange
            var inputofSDRspercycle = new Dictionary<double, List<int[]>>()
            {
                { 1.0, new List<int[]>() { new int[] { 1, 2, 3 }, new int[] { 1, 2, 3, 4 } } },
                { 2.0, new List<int[]>() { new int[] { 4, 5, 6 }, new int[] { 4, 5 } } },
                // Add more entries as needed
             };
            bool c = false;

            // Act
            var result = ComparingOfSDRsForEachCyclePerInput(inputofSDRspercycle, c);

            // Assert
            Assert.IsFalse(result);
        }
        public bool ComparingOfSDRsForEachCyclePerInput(Dictionary<double, List<int[]>> inputofSDRspercycle, bool c)
        {
            foreach (var input in inputofSDRspercycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                int lengthOfList = values.Count;
                int[] array1 = values[lengthOfList - 1];
                int[] array2 = values[lengthOfList - 2];

                if (array1.Length == array2.Length)
                {
                    for (int j = 0; j < array1.Length; j++)
                    {
                        if (array1[j] == array2[j])
                        {
                            if (j == array1.Length - 1)
                            {
                                c = true;
                            }
                        }
                        else
                        {
                            c = false;
                            break;

                        }
                    }
                    if (c == false) break;
                }
                else
                {
                    c = false;
                    break;
                }
            }
            return c;
        }
    }
}