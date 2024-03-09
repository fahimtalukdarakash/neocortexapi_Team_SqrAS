using NeoCortexApi;
using NeoCortexApi.Encoders;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using NeoCortexApi.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            double minOctOverlapCycles = 1.0;
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
                DutyCyclePeriod = 100,
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

            // Will hold the SDR of every inputs.
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

            // To take the value of the dictionary, in which cycle program will break
            int cycle2 = 0;
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

                    similarity = MathHelpers.CalcArraySimilarity(activeColumns, prevActiveCols[input]);

                    Debug.WriteLine($"[cycle={cycle.ToString("D4")}, N={countForCycle}, i={input}, cols=:{actCols.Length} s={similarity}, stable for {countForCycle} cycles] SDR: {Helpers.StringifyVector(actCols)}");

                    //Dictionary, Inpput save if the isInStableState is true
                    //Without the stable value dictionary values will not be saved and shows no value
                    if (isInStableState == true)
                    {
                        inputofSDRspercycle[input].Add(actCols);
                    }

                    prevActiveCols[input] = activeColumns;
                    prevSimilarity[input] = similarity;
                }
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
                            //Console.WriteLine($"{i} : {Helpers.StringifyVector(SDRarray)}");
                        }
                    }
                    else
                    {
                        //Checking if every input has array or not.
                        minimumArray++;
                        if (minimumArray == 2)
                        {
                            Console.WriteLine("all input have minimum 2 arrays");
                        }
                        Console.WriteLine("full SDR of inputs are done");
                        break;
                    }

                }
                if(SDRofallinputs == true && isInStableState == false)
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
                    //Console.WriteLine("full SDR of input done");
                }
                if (SDRofallinputs == true && minimumArray >= 2)
                {

                    foreach (var input in inputofSDRspercycle)
                    {
                        double i = input.Key;
                        List<int[]> values = input.Value;
                        int lengthOfList = values.Count;
                        int[] array1 = values[lengthOfList - 1];
                        int[] array2 = values[lengthOfList - 2];
                        //checking the length of array of the input for put them in the dictionary
                        if (array1.Length == array2.Length)
                        {
                            Console.WriteLine($"{i} : {Helpers.StringifyVector(array1)}....{array1.Length}");
                            Console.WriteLine($"{i} : {Helpers.StringifyVector(array2)}....{array2.Length}");
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
                    if (c == true)
                    {
                        countForCycle++;
                    }
                    else
                    {
                        countForCycle = 0;
                    }
                }
                Console.WriteLine(countForCycle);
                
                //When the cycle count match with given minimum number of cycles then the program will break
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
            foreach (var input in inputofSDRspercycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                int cycle = cycle2;
                Debug.WriteLine($"input:{i}");
                foreach (var SDRarray in values)
                {
                    Debug.WriteLine($"cycle {cycle}:{Helpers.StringifyVector(SDRarray)}");
                    cycle--;
                }
            }
            Debug.WriteLine("Final SDR of all inputs");
            foreach (var input in inputofSDRspercycle)
            {
                double i = input.Key;
                List<int[]> values = input.Value;
                Debug.WriteLine($"{i} : {Helpers.StringifyVector(values[values.Count - 1])}");
            }
            return sp;
       

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
    }
}