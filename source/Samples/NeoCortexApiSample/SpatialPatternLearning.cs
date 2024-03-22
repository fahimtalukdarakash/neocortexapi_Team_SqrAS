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
            double minOctOverlapCycles = 0.5;
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

            SpatialPatternLearning spl = new SpatialPatternLearning();

            // int numStableCycles = 0;

            //Dictionary Initialization && Value Initialization
            Dictionary<double, List<int[]>> inputofSDRspercycle = new Dictionary<double, List<int[]>>();
            foreach (var input in inputs)
            {
                inputofSDRspercycle[input] = new List<int[]>();
            }
            bool SDRofallinputs = false;
            Dictionary<double, int> a = new Dictionary<double, int>();
            int lengthoftotalinputs = inputs.Length;
            int minimumArray = 0;
            int countForCycle = 0;
            int minimumArrayNeededToBreakTheCycle = 100;
            bool c = false;
            int numColumns = 1024;

            int cycle2 = 0;
            Dictionary<double, int> SimilarityOfInput = new Dictionary<double, int>();
            foreach (var input in inputs)
            {
                SimilarityOfInput[input] = 0;
            }
            Dictionary<double, int> StableCycleNumberofEachInput = new Dictionary<double, int>();
            foreach (var input in inputs)
            {
                StableCycleNumberofEachInput[input] = 0;
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
                    
                    int[] arrayOfFullActiveColumns = Enumerable.Repeat(0, numColumns).ToArray(); // Creates an array of integers with a length of 1024 filled with zeroes
                    arrayOfFullActiveColumns = spl.ConvertingZerosIntoOneAtPreferredIndex(arrayOfFullActiveColumns, activeColumns);
                    similarity = MathHelpers.CalcArraySimilarity(activeColumns, prevActiveCols[input]);
                    
                    if ((int)similarity == 100)
                    {
                        SimilarityOfInput[input]++;
                        if (SimilarityOfInput[input] == 50)
                        {
                            StableCycleNumberofEachInput[input] = cycle;
                        }
                    }
                    else
                    {
                        SimilarityOfInput[input] = 0;
                    }

                    Debug.WriteLine($"[cycle={cycle.ToString("D4")}, N={SimilarityOfInput[input]}, i={input}, cols=:{actCols.Length} s={similarity}, stable for {countForCycle} cycles] SDR: {Helpers.StringifyVector(actCols)}");
                    
                    int[,] twoDimArrayofInput = ArrayUtils.Make2DArray<int>(arrayOfFullActiveColumns, (int)Math.Sqrt(numColumns), (int)Math.Sqrt(numColumns));
                    
                    spl.DrawBitMapForInputOfEachCycle(twoDimArrayofInput, input, cycle);
                    //Dictionary, Inpput save if the isInStableState is true
                    //Without the stable value dictionary values will not be saved and shows no value
                    if (isInStableState == true)
                    {
                        inputofSDRspercycle[input].Add(actCols);
                    }

                    prevActiveCols[input] = activeColumns;
                    prevSimilarity[input] = similarity;
                }
                
                spl.PrintingStableCycleNumberOfEachInput(SimilarityOfInput, StableCycleNumberofEachInput, lengthoftotalinputs);
                
                SDRofallinputs = spl.CheckingOfAllInputHaveSDRorNot(inputofSDRspercycle, lengthoftotalinputs, SDRofallinputs);
                if (SDRofallinputs == true) minimumArray++;
                
                if (SDRofallinputs == true && isInStableState == false)
                {
                    foreach (var input in inputofSDRspercycle)
                    {
                        double i = input.Key;
                        List<int[]> values = input.Value;
                        values.Clear();
                        a[i] = 0;
                    }
                    //When the program is in Stable state and then again turns into false 
                    //then we reset all the values so that the values can again start from the beginning
                    SDRofallinputs = false;
                    minimumArray = 0;
                    countForCycle = 0;
                }


                if (SDRofallinputs == true && minimumArray >= 2)
                {

                    c = spl.ComparingOfSDRsForEachCyclePerInput(inputofSDRspercycle, c);
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
            spl.PrintingLast100CyclesSDRofEachInput(inputofSDRspercycle, cycle2);
            Debug.WriteLine("Final SDR of all inputs");
            //Outputs the final column list for each input.
            spl.PrintingFinalSDRofAllInputs(inputofSDRspercycle);
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
        //<summary>
        //Above activeColumns provides only the column number.
        //<parameter name="arrayOfFullActiveColumns">
        //This arrayOfFullActiveColumns will have full 1024 columns, and whichever columns are active will be 1 and the rest will be zero.
        //</parameter>
        //</summary>
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
        //generates BitMaps from cycle 1 to last cycle(till breaks) for each input.
        //parameter "twoDimArrayofInput" is a two-dimensional array of arrayOfFullActiveColumns
        //parameter "input" is for creating the folder for this particular variable
        //parameter "cycle" is for naming the image
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
        
        static Dictionary<double, int> a = new Dictionary<double, int>();
        private bool CheckingOfAllInputHaveSDRorNot(Dictionary<double, List<int[]>> inputofSDRspercycle, int lengthoftotalinputs, bool SDRofallinputs)
        {
            //Dictionary<double, int> a = new Dictionary<double, int>();
            foreach (var input in inputofSDRspercycle)
            {
                //Checking all the input has SDR or not.
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

                    //Console.WriteLine("full SDR of inputs are done 1");
                    break;
                }

            }        
            int count = 0;
            foreach (var b in a)
            {
                if (b.Value == 1)
                {
                    count++;
                    //Console.WriteLine(count);
                }
            }
            if (count == lengthoftotalinputs)
            {
                SDRofallinputs = true;
                //Console.WriteLine("full SDR of input done");
            }
            return SDRofallinputs;
        }
        private bool ComparingOfSDRsForEachCyclePerInput(Dictionary<double, List<int[]>> inputofSDRspercycle, bool c)
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
        private void PrintingFinalSDRofAllInputs(Dictionary<double, List<int[]>> inputofSDRspercycle)
        {
            foreach (var input in inputofSDRspercycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                Debug.WriteLine($"{i} : {Helpers.StringifyVector(values[values.Count - 1])}"); 
            }
        }
        //Refactoring method for printing last 100 cycles of Each input
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
        // Creating the refactoring method for Printing Stable Cycles number of each input
        private void PrintingStableCycleNumberOfEachInput(Dictionary<double, int> SimilarityOfInput, Dictionary<double, int> StableCycleNumberofEachInput, int lengthoftotalinputs)
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
    }
}