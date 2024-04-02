using NeoCortex;
using NeoCortexApi;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NeoCortexApiSample
{
    /// <summary>
    /// Implements an experiment that demonstrates how to learn spatial patterns.
    /// SP will learn every presented input in multiple iterations.
    /// </summary>
    public class SpatialPatternLearning
    {
        public void Run()
        {
            Console.WriteLine($"Hello NeocortexApi! Experiment {nameof(SpatialPatternLearning)}");

            // Used as a boosting parameters
            // that ensure homeostatic plasticity effect.
            double minOctOverlapCycles = 0.45;
            double maxBoost = 5.0;

            // We will use 200 bits to represent an input vector (pattern).
            int inputBits = 200;

            // We will build a slice of the cortex with the given number of mini-columns
            int numColumns = 1024;

            //
            // This is a set of configuration parameters used in the experiment.
            HtmConfig cfg = new HtmConfig(new int[] { inputBits }, new int[] { numColumns })
            {
                CellsPerColumn = 10,
                MaxBoost = maxBoost,
                DutyCyclePeriod = 1000,
                MinPctOverlapDutyCycles = minOctOverlapCycles,

                GlobalInhibition = false,
                NumActiveColumnsPerInhArea = 0.02 * numColumns,
                PotentialRadius = (int)(0.15 * inputBits),
                LocalAreaDensity = -1,
                ActivationThreshold = 10,

                MaxSynapsesPerSegment = (int)(0.01 * numColumns),
                Random = new ThreadSafeRandom(42),
                StimulusThreshold = 10,
            };

            double max = 100;

            //
            // This dictionary defines a set of typical encoder parameters.
            Dictionary<string, object> settings = new Dictionary<string, object>()
            {
                { "W", 15},
                { "N", inputBits},
                { "Radius", -1.0},
                { "MinVal", 0.0},
                { "Periodic", false},
                { "Name", "scalar"},
                { "ClipInput", false},
                { "MaxVal", max}
            };

            EncoderBase encoder = new ScalarEncoder(settings);
                       
            // We are creating here 100 random input values.
            List<double> inputValues = new List<double>();

            for (int i = 0; i < (int)max; i++)
            {
                inputValues.Add((double)i);
            }
          

            var sp = RunExperiment(cfg, encoder, inputValues);

            //RunRustructuringExperiment(sp, encoder, inputValues);
        }
        //Initialized  array elements to 1 in method 

        /// <summary>
        /// Implements the experiment.
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="encoder"></param>
        /// <param name="inputValues"></param>
        /// <returns>The trained bersion of the SP.</returns>
        private static SpatialPooler RunExperiment(HtmConfig cfg, EncoderBase encoder, List<double> inputValues)
        {
            // Creates the htm memory.
            var mem = new Connections(cfg);

            bool isInStableState = false;

            //
            // HPC extends the default Spatial Pooler algorithm.
            // The purpose of HPC is to set the SP in the new-born stage at the begining of the learning process.
            // In this stage the boosting is very active, but the SP behaves instable. After this stage is over
            // (defined by the second argument) the HPC is controlling the learning process of the SP.
            // Once the SDR generated for every input gets stable, the HPC will fire event that notifies your code
            // that SP is stable now.
            HomeostaticPlasticityController hpa = new HomeostaticPlasticityController(mem, inputValues.Count * 40,
                (isStable, numPatterns, actColAvg, seenInputs) =>
                {
                    // Event should only be fired when entering the stable state.
                    // Ideal SP should never enter unstable state after stable state.
                    if (isStable == false)
                    {
                        Debug.WriteLine($"INSTABLE STATE");
                        // This should usually not happen.
                        isInStableState = false;
                    }
                    else
                    {
                        Debug.WriteLine($"STABLE STATE");
                        // Here you can perform any action if required.
                        isInStableState = true;
                    }
                });

            // It creates the instance of Spatial Pooler Multithreaded version.
            SpatialPooler sp = new SpatialPooler(hpa);
            //sp = new SpatialPoolerMT(hpa);

            // Initializes the 
            sp.Init(mem, new DistributedMemory() { ColumnDictionary = new InMemoryDistributedDictionary<int, NeoCortexApi.Entities.Column>(1) });

            // mem.TraceProximalDendritePotential(true);

            // It creates the instance of the neo-cortex layer.
            // Algorithm will be performed inside of that layer.
            CortexLayer<object, object> cortexLayer = new CortexLayer<object, object>("L1");

            // Add encoder as the very first module. This model is connected to the sensory input cells
            // that receive the input. Encoder will receive the input and forward the encoded signal
            // to the next module.
            cortexLayer.HtmModules.Add("encoder", encoder);

            // The next module in the layer is Spatial Pooler. This module will receive the output of the
            // encoder.
            cortexLayer.HtmModules.Add("sp", sp);

            double[] inputs = inputValues.ToArray();

            // Here the code Will hold the SDR of every inputs.
            Dictionary<double, int[]> prevActiveCols = new Dictionary<double, int[]>();

            // Will hold the similarity of SDKk and SDRk-1 fro every input.
            Dictionary<double, double> prevSimilarity = new Dictionary<double, double>();

            //
            // Initiaize start similarity to zero.
            foreach (var input in inputs)
            {
                prevSimilarity.Add(input, 0.0);
                prevActiveCols.Add(input, new int[0]);
            }

            // Learning process will take 1000 iterations (cycles)
            int maxSPLearningCycles = 1000;

            ///<param name="spl">spl is just a object of class SpatialPatternLearning</param>
            SpatialPatternLearning spl = new SpatialPatternLearning();

            // int numStableCycles = 0;

            //Dictionary Initialization && Value Initialization
            ///<param name="inputOfSDRsPerCycle">
            ///This is a dictionary where this dictionary storing the SDRs of per cycle for each input.
            ///</param>
            Dictionary<double, List<int[]>> inputOfSDRsPerCycle = new Dictionary<double, List<int[]>>();
            //Here at first initializing the value of every input with a empty array.
            foreach (var input in inputs)
            {
                inputOfSDRsPerCycle[input] = new List<int[]>();
            }
            ///<param name="SDRofAllInputs">
            ///this is a boolean variable, it is set to false if all inputs don't have SDR array and set to true if all the inputs have SDR array
            ///</param>
            bool SDRofAllInputs = false;
            ///<param name="lengthOfTotalInputs">
            ///this variable is equal to inputs.length (total inputs)
            ///</param>
            int lengthOfTotalInputs = inputs.Length;
            ///<param name="minimumArray">
            ///For comparing the SDR values of consecutive two cycles, there should at least 2 arrays of SDRs stored in the dictionary. 
            ///minimumArraay is for that reason to count the SDR arrays in the dictionary 
            ///</param>
            int minimumArray = 0;
            ///<param name="countForCycle">
            ///This variable is for counting the cycles, after isInStableState set to true and if comparing of two SDR array is matched then this variable's value will increamented by 1.
            ///</param>
            int countForCycle = 0;
            ///<param name="minimumArrayNeededToBreakTheCycle">
            ///this variable is the main breaking points. After how many cycles of comparing the arrays, we will break the main loop. 
            ///</param>
            int minimumArrayNeededToBreakTheCycle = 100;
            ///<param name="c">
            ///c is just boolean variable is set to false. If length of these two arrays same and all the SDR values of these two arrays are same then this 'c' variable set to true otherwise false.
            ///</param>
            bool c = false;
            ///<param name="numColumns">
            ///how many columns is used here. From above, we can see that here number of columns is used as 1024.
            ///If number of column is used 2048 then here also number of columns should be 2048
            ///</param>
            int numColumns = 1024;
            /// To take the value of the cycle, in which cycle program will break
            int cycle2 = 0;
            ///<param name="SimilarityOfInput">
            ///In this dictionary, increasing the iteration number of an input which similarities is 100%
            ///</param>
            Dictionary<double, int> similarityOfInput = new Dictionary<double, int>();
            // at first initializing every input's value with a value of 0.
            foreach (var input in inputs)
            {
                similarityOfInput[input] = 0;
            }
            ///<param name="StableCycleNumberofEachInput">
            ///In this dictionary, storing the cycle number in which a particular input is getting stable
            /// </param>
            Dictionary<double, int> stableCycleNumberofEachInput = new Dictionary<double, int>();
            // at first initializing every input's value with a value of 0
            foreach (var input in inputs)
            {
                stableCycleNumberofEachInput[input] = 0;
            }
            for (int cycle = 0; cycle < maxSPLearningCycles; cycle++)
            {
                Debug.WriteLine($"Cycle  ** {cycle} ** Stability: {isInStableState}");

                //
                // This trains the layer on input pattern.
                foreach (var input in inputs)
                {
                    double similarity;

                    // Learn the input pattern.
                    // Output lyrOut is the output of the last module in the layer.
                    // 
                    var lyrOut = cortexLayer.Compute((object)input, true) as int[];

                    // This is a general way to get the SpatialPooler result from the layer.
                    var activeColumns = cortexLayer.GetResult("sp") as int[];

                    var actCols = activeColumns.OrderBy(c => c).ToArray();
                    ///<summary>
                    ///above activeColumns gives only the columns number
                    ///<param name="arrayOfFullActiveColumns">
                    ///This arrayOfFullActiveColumns will have full 1024 columns and in that which columns are active that column will be 1 and rest of will be zero
                    ///</param>
                    ///</summary>
                    int[] arrayOfFullActiveColumns = Enumerable.Repeat(0, numColumns).ToArray(); // Creates an array of integers with a length of 1024 filled with zeroes
                    arrayOfFullActiveColumns = spl.ConvertingZerosIntoOneAtPreferredIndex(arrayOfFullActiveColumns, activeColumns);
                    similarity = MathHelpers.CalcArraySimilarity(activeColumns, prevActiveCols[input]);
                    ///<summary>
                    ///At first if the similarity is 100% then increasing the value of SimilarityOfInput's value for that particular input
                    ///Then if that iterations number is increased to 50 then we are storing that cycle number in which the input gets into similarity of 100 for 50 consecutive cycles
                    ///If some input's similarity is not 100% then giving the value 0 again for that particular input.
                    ///For example, if input 2 has similarity of 100% for 49 consecutive cycles that means SimilarityOfInput[2]=49, 
                    ///and in the 50th cycle the similarity is not 100% then the value of SimilarityOfInput[2]=0 again.
                    /// </summary>
                    if ((int)similarity == 100)
                    {
                        similarityOfInput[input]++;
                        if (similarityOfInput[input] == 50)
                        {
                            stableCycleNumberofEachInput[input] = cycle;
                        }
                    }
                    else
                    {
                        similarityOfInput[input] = 0;
                    }

                    Debug.WriteLine($"[cycle={cycle.ToString("D4")}, N={similarityOfInput[input]}, i={input}, cols=:{actCols.Length} s={similarity}, stable for {countForCycle} cycles] SDR: {Helpers.StringifyVector(actCols)}");
                    ///<summary>
                    ///<param name="twoDimArrayofInput">
                    ///converting the arrayOfFullActiveColumns into two dimensional array
                    /// </param>
                    ///</summary>
                    int[,] twoDimArrayofInput = ArrayUtils.Make2DArray<int>(arrayOfFullActiveColumns, (int)Math.Sqrt(numColumns), (int)Math.Sqrt(numColumns));
                    ///<summary>
                    ///In this function, generating BitMaps for each input in every cycle so that we can understand how SDRs are changing for input in each cycle.
                    /// </summary>
                    spl.DrawBitMapForInputOfEachCycle(twoDimArrayofInput, input, cycle);
                    //Dictionary, Inpput save if the isInStableState is true
                    //Without the stable value dictionary values will not be saved and shows no value
                    if (isInStableState == true)
                    {
                        inputOfSDRsPerCycle[input].Add(actCols);
                    }

                    prevActiveCols[input] = activeColumns;
                    prevSimilarity[input] = similarity;
                }

                ///<summary>
                ///In this function, printing the cycle in which an input gets stable as well as how many inputs are stable in that cycle.
                /// </summary>
                spl.PrintingStableCycleNumberOfEachInput(similarityOfInput, stableCycleNumberofEachInput, lengthOfTotalInputs);

                ///<summary>
                ///Here, checking whether all the inputs have SDR columns or not.
                ///The reason behind this is, stability is checked by input wise not cycle wise so isInStableState can be set to true in the middile of a cycle.
                ///When isInStableState sets true, at first time some inputs will not have the SDRs and rest of the inputs will have SDRs.
                ///So we have to check whether all the input has SDRs or not.
                ///Here, also checking whether a input have minimum two arrays of SDRs or not because for comparing the SDRs for one input per cycle, need minimum two arrays of SDRs.
                ///<param name="inputofSDRspercycle">
                ///This is a dictionary where this dictionary storing the SDRs of per cycle for each input.
                ///</param>
                ///</summary>
                SDRofAllInputs = spl.CheckingOfAllInputHaveSDRorNot(inputOfSDRsPerCycle, lengthOfTotalInputs, SDRofAllInputs);
                if (SDRofAllInputs == true) minimumArray++;
                ///<summary>
                ///Sometimes when isInStableState variable turns true after some cycles it agains turns false that means at that time the variable SDRofallinputs=true and isInStableState=false
                ///So when isInStableState = false, have to clear all the previous stored value of the dictionary as well as setting the necessery values to it's initial value
                ///<param name="countForCycle">
                ///This variable is for counting the cycles, after isInStableState set to true and if comparing of two SDR array is matched then this variable's value will increamented by 1.
                ///</param>
                ///So when isInStableState turns false again, have to set the value of countForCycle to 0.
                ///</summary>
                if (SDRofAllInputs == true && isInStableState == false)
                {
                    foreach (var input in inputOfSDRsPerCycle)
                    {
                        double i = input.Key;
                        List<int[]> values = input.Value;
                        values.Clear();
                        a[i] = 0;
                    }
                    //When the program is in Stable state and then again turns into false 
                    //then we reset all the values so that the values can again start from the beginning
                    SDRofAllInputs = false;
                    minimumArray = 0;
                    countForCycle = 0;
                }

                if (SDRofAllInputs == true && minimumArray >= 2)
                {

                    c = spl.ComparingOfSDRsForEachCyclePerInput(inputOfSDRsPerCycle, c);
                    if (c == true)
                    {
                        countForCycle++;
                    }
                    else
                    {
                        countForCycle = 0;
                    }
                }

                //Condition checking When the cycle count match with given minimum number of cycles then the program will break
                if (countForCycle == minimumArrayNeededToBreakTheCycle)
                {
                    cycle2 = cycle;
                    break;
                }
                /*if (isInStableState)
                {
                    numStableCycles++;
                }

                if (numStableCycles > 5)
                    break;*/
            }
            // This function prints the dictionary containing the last 100 cycles' SDR values for each input.
            spl.PrintingLast100CyclesSDRofEachInput(inputOfSDRsPerCycle, cycle2);
            Debug.WriteLine("Final SDR of all inputs");
            //Outputs the final column list for each input.
            spl.PrintingFinalSDRofAllInputs(inputOfSDRsPerCycle);
            spl.PrintingAllTheColumnOfWhichInputsWillBeActivated(inputOfSDRsPerCycle, numColumns);
            spl.DrawBitMapOfConnectedInputBitsForColumns(mem);
            return sp;
        }
        private void RunRustructuringExperiment(SpatialPooler sp, EncoderBase encoder, List<double> inputValues)
        {
            foreach (var input in inputValues)
            {
                var inpSdr = encoder.Encode(input);

                var actCols = sp.Compute(inpSdr, false);

                var probabilities = sp.Reconstruct(actCols);

                Debug.WriteLine($"Input: {input} SDR: {Helpers.StringifyVector(actCols)}");

                Debug.WriteLine($"Input: {input} SDR: {Helpers.StringifyVector(actCols)}");
            }
        }
        ///<summary>
        ///Above activeColumns provides only the column number.
        ///</summary>
        ///<param name="arrayOfFullActiveColumns">
        ///This arrayOfFullActiveColumns will have full 1024 columns, and whichever columns are active will be 1 and the rest will be zero.
        ///</param>
        private int[] ConvertingZerosIntoOneAtPreferredIndex(int[] arrayOfFullActiveColumns, int[] activeColumns)
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
        /// generates BitMaps from cycle 1 to last cycle(till breaks) for each input
        /// </summary>
        /// <param name="twoDimArrayofInput">is a two-dimensional array of arrayOfFullActiveColumns</param>
        /// <param name="input">is for creating the folder for this particular variable</param>
        /// <param name="cycle">is for naming the image</param>
        private void DrawBitMapForInputOfEachCycle(int[,] twoDimArrayofInput, double input, int cycle)
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

        //Checking if all inputs have SDR columns.
        //The reason for this is that stability is checked input-wise rather than cycle-wise, so isInStableState can be set to true during the middle of a cycle.
        //When isInStableState is set to true, some inputs will initially lack SDRs while the rest will have SDRs.
        //So we need to determine whether all of the input contains SDRs or not.
        //Also, check whether an input has at least two arrays of SDRs, as comparing the SDRs for one input per cycle requires at least two arrays of SDRs.
        //This is a dictionary that stores the number of SDRs per cycle for each input.
        //This is another dictionary that stores 0 and 1 for each input based on whether the input has an SDR array or not by checking the SDR array length.
        //If an input contains an SDR array, the length of the array must be greater than zero. If the length of the SDR array is greater than zero, then a[input]=1 or a[input]=0.        

        static Dictionary<double, int> a = new Dictionary<double, int>();
        private bool CheckingOfAllInputHaveSDRorNot(Dictionary<double, List<int[]>> inputOfSDRsPerCycle, int lengthOfTotalInputs, bool SDRofAllInputs)
        {
            //Dictionary<double, int> a = new Dictionary<double, int>();
            foreach (var input in inputOfSDRsPerCycle)
            {
                //Checking all the input has SDR or not.
                if (SDRofAllInputs == false)
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

                    //Console.WriteLine("full SDR of inputs are done 1");
                    break;
                }

            }
            ///<summary>
            ///<param name="count">
            ///From the dictionary 'a', getting whether a[input] = 1 or a[input] = 0 
            ///if a[input] = 1 then count will be added by 1 and if all a[input] = 1 then total count will be equal to length of total inputs. 
            ///When count is equal to length of total inputs then setting the variable SDRofallinputs=true.
            ///</param>
            ///<param name="SDRofAllInputs">
            ///this is a boolean variable, it is set to false if all inputs don't have SDR array and set to true if all the inputs have SDR array
            ///</param>
            ///<param name="lengthoftotalinputs">
            ///this variable is equal to inputs.length (total inputs)
            ///</param>
            ///</summary>
            int count = 0;
            foreach (var b in a)
            {
                if (b.Value == 1)
                {
                    count++;
                    //Console.WriteLine(count);
                }
            }
            if (count == lengthOfTotalInputs)
            {
                SDRofAllInputs = true;
                //Console.WriteLine("full SDR of input done");
            }
            return SDRofAllInputs;
        }
        
        ///<param name="inputOfSDRsPerCycle">
        ///This is a dictionary that contains the SDRs per cycle for each input.
        ///</param>
        ///<param name="array1">
        ///array1 is the final SDR array of the particular that is stored..
        ///</param>
        ///<param name="array2">
        ///array2 is the previous cycles SDR array of the previous SDR array.
        ///For example, suppose input 0 contains a 10-cycle SDR array. array1 will be the SDR array of cycle 10, and array 2 will be the SDR array of cycle 9.
        ///</param>
        ///<param name="c">
        ///C is a boolean variable that is set to false. If the lengths of these two arrays are the same, as are all of their SDR values, then this 'c' variable is set to true; otherwise, it is set to false.
        ///</param>
        ///When SDRofallinputs=true and the minimumArray count is 2, we begin the comparison.
        ///<param name="countForCycle">
        ///This variable counts cycles after isInStableState is set to true, and if two SDR arrays are compared, the value of this variable increases by one.
        ///</param>
        ///<summary>
        ///So, if 'c' returns true, this indicates that all of the conditions for comparing the two arrays have been met, and we are incrementing the variable countForCycle by one.
        ///If 'c' is false, we set the variable countForCycle to zero.
        ///</summary>
        private bool ComparingOfSDRsForEachCyclePerInput(Dictionary<double, List<int[]>> inputOfSDRsPerCycle, bool c)
        {
            foreach (var input in inputOfSDRsPerCycle)
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
        private void PrintingFinalSDRofAllInputs(Dictionary<double, List<int[]>> inputOfSDRsPerCycle)
        {
            foreach (var input in inputOfSDRsPerCycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                Debug.WriteLine($"{i} : {Helpers.StringifyVector(values[values.Count - 1])}"); 
            }
        }
        /// <summary>
        /// Here in this function, printing last 100 cycles column list for each input
        /// </summary>
        /// <param name="inputofSDRspercycle">This is a dictionary where this dictionary storing the SDRs of per cycle for each input.</param>
        /// <param name="cycle2">this is the cycle number in which the cycle breaks</param>
        private void PrintingLast100CyclesSDRofEachInput(Dictionary<double, List<int[]>> inputofSDRspercycle, int cycle2)
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
        /// Here in this function, printing whether a input is getting stable or not and if a input gets stable then which cycle, it gets stable that is shown
        /// As well as, in one cycle how many inputs are stable that is also shown.
        /// </summary>
        /// <param name="SimilarityOfInput"> the iteration number of an input which similarities is 100%</param>
        /// <param name="StableCycleNumberofEachInput"> the cycle number in which a particular input is getting stable</param>
        /// <param name="lengthoftotalinputs">this variable is equal to inputs.length (total inputs)</param>
        private void PrintingStableCycleNumberOfEachInput(Dictionary<double, int> SimilarityOfInput, Dictionary<double, int> StableCycleNumberofEachInput, int lengthOfTotalInputs)
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
            double stabilityPercentageOfCycle = ((double)count2 / lengthOfTotalInputs) * 100;
            Debug.WriteLine($"{stabilityPercentageOfCycle}% stable");
        }
        //In this finction, for which input which columns are activating is defined 
        //Ex: Suppose mini coulmn 0 is activated for input 3,,6,8,11,23,88. From this function, this can be defined for all columns and also can be represented
        private void PrintingAllTheColumnOfWhichInputsWillBeActivated(Dictionary<double, List<int[]>> inputOfSDRsPerCycle, int numColumns)
        {
            Dictionary<double, int[]> finalSDRofAllInputs = new Dictionary<double, int[]>();
            foreach (var input in inputOfSDRsPerCycle)
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
        //Drawing Bitmaps of connected input bits for each column
        private void DrawBitMapOfConnectedInputBitsForColumns(Connections mem)
        {
            SpatialPooler sp = new SpatialPooler();
            SpatialPatternLearning spl = new SpatialPatternLearning();
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