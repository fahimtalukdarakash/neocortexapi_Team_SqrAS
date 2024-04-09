using NeoCortex;
using NeoCortexApi.Entities;
using NeoCortexApi.Utility;
using NeoCortexApi;
using ScottPlot.Palettes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
/// <summary>
/// Here in this function, generating BitMaps from cycle 1 to last cycle(where it breaks) for each input.
/// </summary>
/// <param name="twoDimArrayofInput">Two-dimensional array of arrayOfFullActiveColumns</param>
/// <param name="input">this is for creating the floder for this particular variable</param>
/// <param name="cycle">this is for naming the image</param>
public void DrawBitMapForInputOfEachCycle(int[,] twoDimArrayofInput, double input, int cycle)
        {
            string basePath = Path.Combine(Environment.CurrentDirectory, "Outputs");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            string desiredPath = basePath;
            string folderNameForInput = input.ToString();
            string fullPath = Path.Combine(desiredPath, folderNameForInput);
            if (!Directory.Exists(fullPath))
            {
                // If it doesn't exist, create it
                Directory.CreateDirectory(fullPath);
                NeoCortexUtils.DrawBitmap(twoDimArrayofInput, 10, $"{fullPath}\\{cycle}.png", Color.Black, Color.Red, text: input.ToString());
            }
            else
            {
                NeoCortexUtils.DrawBitmap(twoDimArrayofInput, 10, $"{fullPath}\\{cycle}.png", Color.Black, Color.Red, text: input.ToString());
            }
        }

        ///<summary>
        ///<param name="inputofSDRspercycle">
        ///This is a dictionary where this dictionary storing the SDRs of per cycle for each input.
        ///</param>
        ///<param name="array1">
        ///array1 is the last SDR array of the particular that is stored.
        ///</param>
        ///<param name="array2">
        ///array2 is the previous cycles SDR array of the last SDR array
        ///for example, if input 0 has 10 cycles SDR array. array1 will be SDR array of cycle 10 and array 2 will be SDR array of cycle 9
        ///</param>
        ///<param name="c">
        ///c is just boolean variable is set to false. If length of these two arrays same and all the SDR values of these two arrays are same then this 'c' variable set to true otherwise false.
        ///</param>
        ///Here when SDRofallinputs= true and minimumArray count is 2 then we are starting the comparing.
        ///<param name="countForCycle">
        ///This variable is for counting the cycles, after isInStableState set to true and if comparing of two SDR array is matched then this variable's value will increamented by 1.
        ///</param>
        ///So if 'c' is turns true that means all the condition of coparing the two arrays is matched and we are increamenting the variable countForCycle by 1.
        ///if 'c' is false, we are setting the variable countForCycle to 0.
        ///</summary>
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
        /// <summary>
        /// Here in this function, printing last 100 cycles column list for each input
        /// </summary>
        /// <param name="inputofSDRspercycle">This is a dictionary where this dictionary storing the SDRs of per cycle for each input.</param>
        /// <param name="cycle2">this is the cycle number in which the cycle breaks</param>
        public void PrintingLast100CyclesSDRofEachInput(Dictionary<double, List<int[]>> inputofSDRspercycle, int cycle2)
        {
            foreach (var input in inputofSDRspercycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                int cycle = cycle2 - 102;
                Debug.WriteLine($"input:{i}");
                foreach (var SDRarray in values)
                {
                    Debug.WriteLine($"cycle {cycle}:{Helpers.StringifyVector(SDRarray)}");
                    cycle++;
                }
            }
        }
        /// <summary>
        /// Just printing the final SDR of each input
        /// </summary>
        /// <param name="inputofSDRspercycle">This is a dictionary where this dictionary storing the SDRs of per cycle for each input.</param>
        public void PrintingFinalSDRofAllInputs(Dictionary<double, List<int[]>> inputofSDRspercycle)
        {
            foreach (var input in inputofSDRspercycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                Debug.WriteLine($"{i} : {Helpers.StringifyVector(values[values.Count - 1])}");
            }
        }
        /// <summary>
        /// Here in this function, printing whether a input is getting stable or not and if a input gets stable then which cycle, it gets stable that is shown
        /// As well as, in one cycle how many inputs are stable that is also shown.
        /// </summary>
        /// <param name="SimilarityOfInput"> the iteration number of an input which similarities is 100%</param>
        /// <param name="StableCycleNumberofEachInput"> the cycle number in which a particular input is getting stable</param>
        /// <param name="lengthoftotalinputs">this variable is equal to inputs.length (total inputs)</param>
        public void PrintingStableCycleNumberOfEachInput(Dictionary<double, int> SimilarityOfInput, Dictionary<double, int> StableCycleNumberofEachInput, int lengthoftotalinputs)
        {
            int count2 = 0;
            foreach (var input in SimilarityOfInput)
            {
                double i = input.Key;
                int value = input.Value;
                int value2 = StableCycleNumberofEachInput[i];
                if (value >= 50)
                {
                    Debug.WriteLine($"input {i}: Stable Input and stable on {value2} cycle");
                    count2++;
                }
                else
                {
                    Debug.WriteLine($"input {i}: Not stable Input ");
                }
            }
            double stabilityPercentageOfCycle = ((double)count2 / lengthoftotalinputs) * 100;
            Debug.WriteLine($"{stabilityPercentageOfCycle}% stable");
        }
        public void PrintingAllTheColumnOfWhichInputsWillBeActivated(Dictionary<double, List<int[]>> inputofSDRspercycle, int numColumns)
        {
            Dictionary<double, int[]> finalSDRofAllInputs = new Dictionary<double, int[]>();
            foreach (var input in inputofSDRspercycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                finalSDRofAllInputs.Add(i, values[values.Count - 1]);
            }
            Dictionary<int, List<double>> InputsWillActivatedForAColumn = new Dictionary<int, List<double>>();
            for (int i = 0; i < numColumns; i++)
            {
                InputsWillActivatedForAColumn[i] = new List<double>();
            }
            foreach (var input in finalSDRofAllInputs)
            {
                double i = input.Key;
                int[] values = input.Value;
                foreach (var value in values)
                {
                    InputsWillActivatedForAColumn[value].Add(i);
                }
            }
            foreach (var column in InputsWillActivatedForAColumn)
            {
                int i = column.Key;
                List<double> values = column.Value;
                Debug.WriteLine($"{i} : {Helpers.StringifyVector(values)}");
            }
        }
        public void DrawBitMapOfConnectedInputBitsForColumns(Connections mem)
        {
            SpatialPooler sp = new SpatialPooler();
            SPLHelpers spl = new SPLHelpers();
            Dictionary<int, List<int>> columnsConnectedWithInputBits = sp.ConnectedInputBits(mem);
            foreach (var column in columnsConnectedWithInputBits)
            {
                int i = column.Key;
                List<int> values = column.Value;
                Debug.WriteLine($"{i} : {Helpers.StringifyVector(values)}");
            }
            string basePath = Path.Combine(Environment.CurrentDirectory, "OutputInputBits");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            int[] arrayOfFullInputBits = new int[mem.HtmConfig.NumInputs];
            foreach (var column in columnsConnectedWithInputBits)
            {
                int i = column.Key;
                List<int> values = column.Value;
                int[] inputBits = values.ToArray();
                arrayOfFullInputBits = Enumerable.Repeat(0, mem.HtmConfig.NumInputs).ToArray(); // Creates an array of integers with a length of 1024 filled with zeroes
                arrayOfFullInputBits = spl.ConvertingZerosIntoOneAtPreferredIndex(arrayOfFullInputBits, inputBits);
                int[,] twoDimArrayofInputBits = ArrayUtils.Make2DArray<int>(arrayOfFullInputBits, (int)Math.Sqrt(mem.HtmConfig.NumInputs), (int)Math.Sqrt(mem.HtmConfig.NumInputs));
                NeoCortexUtils.DrawBitmap2(twoDimArrayofInputBits, 20, $"{basePath}\\column {i}.png", Color.Black, Color.Red, text: i.ToString());
            }
        }

    }
}
