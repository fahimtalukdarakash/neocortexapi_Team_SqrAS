# ML 23/24-03 Implement the new Spatial Learning experiment
Group Name: Team_SqrAs [Main Branch](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS)

Implementation of New Spatial Pattern Learning : [Spatial Pattern Learning](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs)

## Project Description:
The SpatialPatternLearning experiment shows the application of the NeoCortexApi library in training a spatial pooler to learn and recognize spatial patterns. Inspired by biological neural networks and developed within the field of artificial intelligence, the experiment explores the mechanics behind spatial pattern recognition, a key component of cognitive processing.

**Project Guideline:**
The current SpatialPatternLearning experiment runs forever (maxSPLearningCycles). The new code should persist SDRs of all inputs in the dictionary, like
Dictionary<input, sdr> -> Dictionary<int, List<int>>
Input is the key, which is the scalar value. The value in the dictionary is the list of integers that represent the SDR (currently active mini-columns). During learning time, the SP will create different SDRs for the same input. After a while, it will keep the SDR stable.

Our code should ensure that, after the variable isInStableState is set to true, NO SDR change is detected.
To solve this, we might track the last iteration (cycle) in which the SDR of the certain input was set.
The experiment should exit after the SDR does not change for the given number of iterations.
Also needed to find a better way to output the result. 

For example,
Cycle N – How many iterations the SDR for the input was not changes (is stable)
instead of
[cycle=0419, i=46, cols=:20 s=100] SDR: 357, 362, 363, 379, 391
Use an additional value stable X cycles.
[cycle=0419,N= 10, i=46, cols=:20 s=100, stable 7 cycles] SDR: 357, 362, 363, 379, 391

## 1. Objectives:
Based on the project description, We have subdivided our tasks and the list of those tasks are as below:
- **Task 1:** Analyzing the code of spatial pattern learning
- **Task 2:** Investing the reason, why first 40 cycles don't give mini columns list for inputs 51 to 99.
- **Task 3:** Implementing new spatial pattern learning experiment.
- **Task 4:** Write out the dictionary at the end
- **Task 5:** Providing some more readable statistical info about the stability of all mini-columns.

## Approach

### Task 1: Analyzing the code of spatial pattern learning
In currently implemented SpatialPatternLearning algorithm, at first it is initialzing the necessary values of `HtmConfig` and `Encoder`. After this setup, 100 random input values was created and then `HtmConfig`, `Encoder` and `inputValues` are passed into `var sp = RunExperiment(cfg, encoder, inputValues)` this method which returns column list of every input values by using spatial pooler algorithm.
```
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
                StimulusThreshold=10,
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

            //
            // We create here 100 random input values.
            List<double> inputValues = new List<double>();

            for (int i = 0; i < (int)max; i++)
            {
                inputValues.Add((double)i);
            }

            var sp = RunExperiment(cfg, encoder, inputValues);


```

In spatial pooler algorithm, at first it establish the connections by `HtmConfig` parameters as well as creates the memory. 
```
// Creates the htm memory.
var mem = new Connections(cfg);
```
The variable `isInStableState` is set to false because at first when the program will give the column list of input that will not be stable and it need to be in stable state for each input. That's why, in spatial pooler algorithm, homeostatic plasticity controller algorithm is used. `HomeostaticPlasticityController` extends the default Spatial Pooler algorithm. The purpose of `HomeostaticPlasticityController` is to set the SP in the new-born stage at the begining of the learning process. In this stage the boosting is very active, but the SP behaves instable. After this stage is over, the `HomeostaticPlasticityController` will be controlling the learning process of the SP. Once the SDR generated for every input gets stable, the `HomeostaticPlasticityController` will fire event that notifies the code that SP is stable now that means `isInStableState` will set to true.

```
bool isInStableState = false;
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

```

As we know that the Hierarchical Temporal Memory have three layers. **Encoder**, **Spatial Pooler** and **Temporal Memory**. 

In a more precise description of SpatialPatternLearning, it shows how actually Spatial Pooler learn patterns when the input is encoded by the encoder. So in this program, two layers(**Encoder** and **Spatial Pooler**) is involved. This is done by adding the `cortexLayer`, at first `cortexLayer` will add Encoder then it will add Spatial Pooler.
```
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
```

After this, the existing algorithm is taking 1000 iterations for the learning process. 
```
int maxSPLearningCycles = 1000;
```
In the learning process, at first each input is encoded by `Scalar Encoder Algorithm` then for each input, which columns will be activated,that is done by the spatial pooler algorithm and the column list stored in `activeColumns`. For the first 40 iterations, the column list of each input will be unstable and `HomeostaticPlasticityController` will control these column list. Those inputs have columns, `HomeostaticPlasticityController` will try to make them stable and those inputs don't have columns, `HomeostaticPlasticityController` will boost them so that every input have activated columns. After 40 iterations, all input will have column list and `HomeostaticPlasticityController` will make them stable so that for each input the activation of columns doesn't change. 

The algorithm `HomeostaticPlasticityController` is getting onto stable state = true for each input, if that input has similarity of 97% column list for consecutive 50 cycles. 
```
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

                    Debug.WriteLine($"[cycle={cycle.ToString("D4")}, i={input}, cols=:{actCols.Length} s={similarity}] SDR: {Helpers.StringifyVector(actCols)}");

                    prevActiveCols[input] = activeColumns;
                    prevSimilarity[input] = similarity;
                }

                if (isInStableState)
                {
                    numStableCycles++;
                }

                if (numStableCycles > 5)
                    break;
            }
```
At last, currently the existing program checking the boolean variable `isInStableState` is set to true or not for 5 cycles then breaks the loop.

### Task 2: Investegating the reason, why first 40 cycles don't give mini-columns list of SDR for inputs 51 to 99.
After analyzing the code, we thought the problem is in `HomeostaticPlasticityController`. To verify that, we ran the program without `HomeostaticPlasticityController` and found that the problem is not there.
Then we tried to understand the Spatial Pooler Algorithm to find out the problem. We understood that the inhibition algorithm is might be the reason for not inhibiting mini columns SDR list. In this experiment for Spatial Pattern Learning Experiment, the local inhibition was used. We saw that there are in total 6 different local inhibition algorithm implemented.

```
public virtual int[] InhibitColumnsLocal(Connections c, double[] overlaps, double density)
{
    return InhibitColumnsLocalOriginal(c, overlaps, density);
    //return InhibitColumnsLocalNewApproach(c, overlaps);
    //return inhibitColumnsLocalNewApproach2(c, overlaps, density);
    //return inhibitColumnsLocalNewApproach3(c, overlaps, density);
    //return InhibitColumnsLocalNewApproach11(c, overlaps, density);
    //return InhibitColumnsLocalNew(c, overlaps, density);
}

```
We tried all the algorithms one by one.
 **InhibitColumnsLocalNewApproach(c, overlaps)** this function doesn't take density into account and gives error when there is no  columns for input 51. </br>
 **inhibitColumnsLocalNewApproach2(c, overlaps, density)** this fuction generates all the  columns for input 0 from the beginning. For the first few inputs, it generates too much  columns and after few inputs, the other input's  columns are low and from this function the  columns SDR list are not coming from input 51 to input 99. After cycle 41, it generates a lot of  columns instead of **0.02*numColumns**.</br>
 **inhibitColumnsLocalNewApproach3(c, overlaps, density)** this function doesn't generate any  columns for the inputs.</br>
  **InhibitColumnsLocalNewApproach11(c, overlaps, density)** this function generates  columns from the beginning for all the inputs but the main problem is, the columns SDR list length is also a lot instead of `0.02*numColumns`. </br>
  **InhibitColumnsLocalNew(c, overlaps, density)** it also behaves like **inhibitColumnsLocalNewApproach2(c, overlaps, density)** this function. </br>
  
  From our findings, the best one is **InhibitColumnsLocalOriginal(c, overlaps, density)** which is already implemented, because it generates `0.02*numColumns` length of  columns per input for all the inputs after 40 cycles. But the problem of not generating the columns for input 51 to input 99 for the first 40 cycles was still there. 
As we tried all the inhibition algorithms and found that the problem is not there because inhibition algorithm inhibits columns when there will be minimum one column for one input. So if there are no columns for one input then it will not inhibit the columns for that particular input. Means, those inputs which don't generate columns are not strong enough for those inputs. Hence, we come out to a conclusion that the problem might be in the boosting algorithm where the weak columns are boosted. </br>

So we tried to understand the function where the boosting of low overlap columns are happening. Here is the function:
```
 public virtual void BoostColsWithLowOverlap(Connections c)
 {
     // Get columns with too low overlap.
     var weakColumns = c.Memory.Get1DIndexes().Where(i => c.HtmConfig.OverlapDutyCycles[i] < c.HtmConfig.MinOverlapDutyCycles[i]).ToArray();

     for (int i = 0; i < weakColumns.Length; i++)
     {
         Column col = c.GetColumn(weakColumns[i]);

         Pool pool = col.ProximalDendrite.RFPool;
         double[] perm = pool.GetSparsePermanences();
         ArrayUtils.RaiseValuesBy(c.HtmConfig.SynPermBelowStimulusInc, perm);
         int[] indexes = pool.GetSparsePotential();

         col.UpdatePermanencesForColumnSparse(c.HtmConfig, perm, indexes, true);
         //UpdatePermanencesForColumnSparse(c, perm, col, indexes, true);
     }
 }
```
So, our idea is to reducing the number of weak columns as fast as possible. So after getting the weak columns, we tried to implement another algorithm. We read the paper [The HTM Spatial Pooler—A Neocortical Algorithm for Online Sparse Distributed Coding](https://www.frontiersin.org/articles/10.3389/fncom.2017.00111/full#B14) written by Yuwei Cui, Subutai Ahmad, Jeff hawkins. There we found a mathematical equation of boosting the weak columns. 
![Equation of Boosting Algorithm](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Equation%20of%20Boosting.png)</br>
So, we implement this equation as a new boosting algorithm
```
public virtual void BoostColsWithLowOverlap2(Connections c)
{
    // Get columns with too low overlap.
    var weakColumns = c.Memory.Get1DIndexes().Where(i => c.HtmConfig.OverlapDutyCycles[i] < c.HtmConfig.MinOverlapDutyCycles[i]).ToArray();

    foreach (var weakColumnIndex in weakColumns)
    {
        Column col = c.GetColumn(weakColumnIndex);

        // Get the boosting signal for the weak column based on its overlap duty cycle.
        double boostingSignal = CalculateBoostingSignal(col, c.HtmConfig);

        // Adjust the synaptic connections (permanences) associated with the weak column.
        AdjustPermanencesForColumn(col, c.HtmConfig.SynPermBelowStimulusInc, boostingSignal,c);
    }
}

private double CalculateBoostingSignal(Column col, HtmConfig config)
{
    double overlapDutyCycle = config.OverlapDutyCycles[col.Index];
    double minOverlapDutyCycle = config.MinOverlapDutyCycles[col.Index];
    double BoostAlpha = 100;
    // Calculate the boosting signal based on the difference between the current overlap duty cycle and the minimum overlap duty cycle.
    //double boostingSignal = 1 / (1 + Math.Exp(-BoostAlpha * (overlapDutyCycle - minOverlapDutyCycle)));
    double boostingSignal = Math.Exp(-BoostAlpha * (overlapDutyCycle - minOverlapDutyCycle));

    return boostingSignal;
}

private void AdjustPermanencesForColumn(Column col, double synPermBelowStimulusInc, double boostingSignal, Connections c)
{
    Pool pool = col.ProximalDendrite.RFPool;
    double[] permanences = pool.GetSparsePermanences();

    // Boost the permanences associated with the weak column.
    for (int i = 0; i < permanences.Length; i++)
    {
        permanences[i] += synPermBelowStimulusInc * boostingSignal;
    }

    // Update the permanences for the column.
    col.UpdatePermanencesForColumnSparse(c.HtmConfig, permanences, pool.GetSparsePotential(), true);
}

```
After implementing this, we ran the program but we didn't see any sigficant changes in low overlap columns boosting. 

After applying and implementing all of the previous ideas, we assumed that specifically the problem is maybe in this line:
```
var weakColumns = c.Memory.Get1DIndexes().Where(i => c.HtmConfig.OverlapDutyCycles[i] < c.HtmConfig.MinOverlapDutyCycles[i]).ToArray();
```
In this line of code, if `OverlapDutyCycles[i]` is less than `MinOverlapDutyCycles[i]` then weak column will be detected. So, if we can increase the value of `OverlapDutyCycles[i]` as well as decrease the value of `MinOverlapDutyCycles[i]` then the weak columns will be reduced faster than before. So we saw from where OverlapDutyCycles are calcluated. OverlapDutyCycles values are coming from this function: 
```
public void UpdateDutyCycles(Connections c, int[] overlaps, int[] activeColumns)
{
    // All columns with overlap are set to 1. Otherwise 0.
    double[] overlapFrequencies = new double[c.HtmConfig.NumColumns];

    // All active columns are set on 1, otherwise 0.
    double[] activeColFrequencies = new double[c.HtmConfig.NumColumns];

    //
    // if (sourceA[i] > 0) then targetB[i] = 1;
    // This ensures that all values in overlapCycles are set to 1, if column has some overlap.
    ArrayUtils.GreaterThanXThanSetToYInB(overlaps, overlapFrequencies, 0, 1);

    if (activeColumns.Length > 0)
    {
        // After this step, all rows in activeCycles are set to 1 at the index of active column.
        ArrayUtils.SetIndexesTo(activeColFrequencies, activeColumns, 1);
    }

    int period = c.HtmConfig.DutyCyclePeriod;
    if (period > c.SpIterationNum)
    {
        period = c.SpIterationNum;
    }

    c.HtmConfig.OverlapDutyCycles = CalcEventFrequency(c.HtmConfig.OverlapDutyCycles, overlapFrequencies, period);

    c.HtmConfig.ActiveDutyCycles = CalcEventFrequency(c.HtmConfig.ActiveDutyCycles, activeColFrequencies, period);
}

```
So, from this function, we can see that if we increase the value of the variable `period` then value of `OverlapDutyCycles[i]` will be increased. The value of the variable `period` is coming from `HtmConfig.DutyCyclePeriod`. So if we increase the value of the variable `DutyCyclePeriod` then the `OverlapDutyCycles[i]` will be increased. </br>
The other thing is, we have to decrease the value of `MinOverlapDutyCycles[i]`. In the function below, which is calculating the `MinOverlapDutyCycles[i]`: 
```
public void UpdateMinDutyCyclesLocal(Connections c)
{
    int len = c.HtmConfig.NumColumns;

    Parallel.For(0, len, (i) =>
    {
        int[] neighborhood = GetColumnNeighborhood(c, i, this.InhibitionRadius);

        double maxActiveDuty = ArrayUtils.Max(ArrayUtils.ListOfValuesByIndicies(c.HtmConfig.ActiveDutyCycles, neighborhood));
        double maxOverlapDuty = ArrayUtils.Max(ArrayUtils.ListOfValuesByIndicies(c.HtmConfig.OverlapDutyCycles, neighborhood));

        c.HtmConfig.MinActiveDutyCycles[i] = maxActiveDuty * c.HtmConfig.MinPctActiveDutyCycles;

        c.HtmConfig.MinOverlapDutyCycles[i] = maxOverlapDuty * c.HtmConfig.MinPctOverlapDutyCycles;
    });
}

```
From this function, we are seeing that MinOverlapDutyCycles[i] is calculated by multiplying `maxOverlapDuty` and `MinPctOverlapDutyCycles`. The value of MinPctOverlapDutyCycles is coming from HtmConfig. So if we decrease the value of MinPctOverlapDutyCycles then the value of `MinOverlapDutyCycles[i]` will be decreased also. </br>
So, we tuned the value of `DutyCyclePeriod` and `MinPctOverlapDutyCycles` and we saw that our assumption was right. We are getting the value from cycle 2 to cycle 3 for all the inputs. The observation of tuning the parameters is given [here](https://docs.google.com/spreadsheets/d/1Gt_9ipORZ-UDyoITu21f9JoCJhDZgUQ2c2-MMsRO5lE/edit#gid=447798372) and here we are giving the only one picture where we got the best output. Because we are getting the value from cycle 3 as well as getting the stability faster comparing to other parameter tuning. The updated value of these two variables are `DutyCyclePeriod = 1000` and `MinPctOverlapDutyCycles = MinOctOverlapCycles` where `MinOctOverlapCycles = 0.45`. By using these value we are getting the stability on cycle 384. Here are the figures:
First figure is for getting the SDR values of all inputs in cycle 3.
![Getting the SDRs of input 51 to input 99 from cycle 3](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/from%20Cycle%203.png) 
Second figure is for showing that for this parameter changing the stability gets faster than the previous implementation.
![Getting the stability on cycle 384](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Stable%20on%20384%20cycle.png).

Finally, we can say that if we reduce the weak columns with the balance of tuned parameter, then we will got the best output for this program.

### Task 3: Implementing new spatial pattern learning experiment

After getting the stability on 384 cycle that means the variable `isInStableState = true` then we are storing the column list of each input in the dictionary. The dictionary is storing column list like this: 
`input:0`
`cycle 384:7, 24, 29, 43, 46, 59, 62, 70, 102, 112, 114, 116, 118, 148, 154, 155, 933, 953, 982, 1012,` 
`cycle 383:7, 24, 29, 43, 46, 59, 62, 70, 102, 112, 114, 116, 118, 148, 154, 155, 933, 953, 982, 1012,`
`input:1`
`cycle 384:7, 12, 24, 29, 37, 39, 46, 54, 59, 95, 102, 118, 125, 148, 154, 155, 953, 961, 998, 1012,`
`cycle 383:7, 12, 24, 29, 37, 39, 46, 54, 59, 95, 102, 118, 125, 148, 154, 155, 953, 961, 998, 1012,`

So after the variable `isInStableState = true` we are storing the column list and comparing the column list for another 100 cycles whether there is any change happens or not.
At first we are checking whether all the column list of inputs are stored or not
```
///<summary>
///Here, checking whether all the inputs have SDR columns or not.
///The reason behind this is, stability is checked by input wise not cycle wise so isInStableState can be set to true in the middile of a cycle.
///When isInStableState sets true, at first time some inputs will not have the SDRs and rest of the inputs will have SDRs.
///So we have to check whether all the input has SDRs or not.
///Here, also checking whether a input have minimum two arrays of SDRs or not because for comparing the SDRs for one input per cycle, need minimum two arrays of SDRs.
///<param name="inputofSDRspercycle">
///This is a dictionary where this dictionary storing the SDRs of per cycle for each input.
///</param>
///<param name="a">
///This is another dictionary, it is storing 0 and 1 for per input based on whether a input has SDR array or not by checking the SDR array length
///If a input has SDR array then the length of that array must be greater than 0 so if the SDR array's length is greater than 0 then that a[input]=1 or a[input]=0
///</param>
///<param name="minimumArray">
///If all the inputs have SDRs then the SDRofallinputs variable will set to true and then it will go to the else part and the counting of minimum array will start.
///And if minimumArray count is 2 then it will break the foreach loop
///</param>
///</summary>
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

///<summary>
///<param name="a">
///This is a dictionary, it is storing 0 and 1 for per input based on whether a input has SDR array or not by checking the SDR array length
///If a input has SDR array then the length of that array must be greater than 0 so if the SDR array's length is greater than 0 then that a[input]=1 or a[input]=0
///</param>
///<param name="count">
///From the dictionary 'a', getting whether a[input] = 1 or a[input] = 0 
///if a[input] = 1 then count will be added by 1 and if all a[input] = 1 then total count will be equal to length of total inputs. 
///When count is equal to length of total inputs then setting the variable SDRofallinputs=true.
///</param>
///<param name="SDRofllinputs">
///this is a boolean variable, it is set to false if all inputs don't have SDR array and set to true if all the inputs have SDR array
///</param>
///<param name="lengthoftotalinputs">
///this variable is equal to inputs.length (total inputs)
///</summary>
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

```

When the variable `isInStableState` is true but after some cycles sometimes it turns into false again. So for that, we have to wait again because `isInStableState` takes 50 cycles to turn it true again. At this time, we are clearing the SDR arrays of per cycle for each input and setting the necessery values to it's initial value. 

```
///<summary>
///Sometimes when isInStableState variable turns true after some cycles it agains turns false that means at that time the variable SDRofallinputs=true and isInStableState=false
///So when isInStableState = false, have to clear all the previous stored value of the dictionary as well as setting the necessery values to it's initial value
///<param name="countForCycle">
///This variable is for counting the cycles, after isInStableState set to true and if comparing of two SDR array is matched then this variable's value will increamented by 1.
///</param>
///So when isInStableState turns false again, have to set the value of countForCycle to 0.
///</summary>
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

```

After `isInStableState=true` and storing SDR arrays of each cycle per input, we are comparing last two cycles SDR array of each input whether two arrays are fully matched or not. We are doing this for every input for consecutive 100 cycles and if it doesn't match then we are setting the the variable `countForCycle` to 0 again and doing the comparison of last two cycles SDR array of each input again. When the conditions met for consecutive 100 cycles, we are breaking the loop.
```
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
if (SDRofallinputs == true && minimumArray >= 2)
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
            //Console.WriteLine($"{i} : {Helpers.StringifyVector(array1)}....{array1.Length}");
            //Console.WriteLine($"{i} : {Helpers.StringifyVector(array2)}....{array2.Length}");
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
///<summary>
///<param name="countForCycle">
///This variable is for counting the cycles, after isInStableState set to true and if comparing of two SDR array is matched then this variable's value will increamented by 1.
///</param>
///<param name="minimumArrayNeededToBreakTheCycle">
///this variable is the main breaking points. After how many cycles of comparing the arrays, we will break the main loop. 
///</param>
///</summary>
//When the cycle count match with given minimum number of cycles to break the cycle then the program will break
if (countForCycle == minimumArrayNeededToBreakTheCycle)
{
    cycle2 = cycle;
    break;
}

``` 
### Task 4: Write out the dictionary at the end
After breaking the cycle, we are printing the dictionary at the end of the runned program.
This is the demo output for input 0 for last 100 cycles of column's SDR that were compared by each cycle. 
![Last 100 stable cycles of column's SDR for input 0](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/demo%20output%20for%20input%200%20for%20100%20cycles%201.png)
![Last 100 stable cycles of column's SDR for input 0](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/demo%20output%20for%20input%200%20for%20100%20cycles%202.png)
![Last 100 stable cycles of column's SDR for input 0](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/demo%20output%20for%20input%200%20for%20100%20cycles%203.png)

Finally, main dictionary is printed.
![Final SDR of all inputs](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/final%20SDR%20all%20inputs%201.png)
![Final SDR of all inputs](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/final%20SDR%20all%20inputs%202.png)
![Final SDR of all inputs](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/final%20SDR%20all%20inputs%203.png)

### Task 5: Providing some more readable statistical info about the stability of all mini-columns.

1. At first, we are showing the output as described in the project description.</br>
Previous Implementation's Output:</br>
![Output of previous implementation](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/previous%20main%20output.png)
After Our Implmentation:</br>
![output as description in the project file](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/main%20output.png)
Here, N = how many iterations the SDR for the input was not changes. After `isInStableState` is true, we are counting and showing how many cycles are stable.

2. After each cycle, we are showing that whether a input is stable or not and if a input is stable then at which cycle it gets stable.
![Input getting stable at which cycle](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Input%20getting%20stable%20at%20which%20cycle%201.png)
![Input getting stable at which cycle](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Input%20getting%20stable%20at%20which%20cycle%202.png)
![Input getting stable at which cycle](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Input%20getting%20stable%20at%20which%20cycle%203.png)</br>
When input is not getting stable,</br>
![Input not getting stable](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Input%20getting%20not%20stable.png)

3. After each cycle, we are showing by percentage that how many inputs are stable for that perticular cycle.</br>
![Stability percentage for all inputs (100% stable)](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Stability%20percentage%20for%20all%20inputs%20in%20a%20cycle%201.png)</br>
![Stability percentage for all inputs (97% stable)](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/images/Stability%20percentage%20for%20all%20inputs%20in%20a%20cycle%202.png)

4. We also generate the bitmap for each input of every cycle so that we can easily viewed how actually the SDRs are changing for inputs per cycle. Below here, given a video of that represention:</br>
[![Bitmap Output](https://github.com/fahimtalukdarakash/weather-showing-project/blob/main/Bitmap%20Output%20for%20input%200.png)](https://www.youtube.com/watch?v=RHf9Xz7jz7A)</br>

Here the image is shown only for input 0 and if you click the image, it will redirect you to the Youtube video which we uploaded where you will get better representation.

5. Here is the google sheet link where we tried to tuned the values of the `HtmConfig` and other variables and see for which value we are getting all the input's SDR from which cycle.
[Parameter tuning](https://docs.google.com/spreadsheets/d/1Gt_9ipORZ-UDyoITu21f9JoCJhDZgUQ2c2-MMsRO5lE/edit#gid=447798372)

## Conclusion
The SpatialPatternLearning experiment has provided invaluable insights into the capabilities and dynamics of spatial pattern learning within hierarchical temporal memory (HTM) systems. Through meticulous experimentation and analysis, we have demonstrated the efficacy of the program. Our project was to implement the new spatial pattern learning where the program should exit from the loop by a certain condition after the variable `isInStableState` is set to true which we have implemented successfully in our program. It ensures that after the variable `isInStableState` is true, then it's stability will not change by checking consecutive 100 cycles. At the end, dictionary is written. It provides, how many iterations the SDR for the input was not changes. After `isInStableState` is true, counting and showing how many cycles are stable. After each cycle, whether a input is stable or not and if a input is stable then at which it gets stable as well as showing by percentage that how many inputs are stable for that cycle. Generating bitmaps for each input for every cycle to represent how actually the SDRs are changing through cycles for each inputs.</br>

 We also investigate why first 40 cycles don't give mini columns list for inputs 51 to 99. After investigating we also tuned the parameter as required and also showed the output of the other combinations. Right now, this method becomes faster than the previous method. Even though we are getting SDRs from Cycle 3 instead of Cycle 40. At last, first few inputs are inhibiting columns more than double of `ColumnPerInhibitionArea`. If it can be reduced for first 40 cycles then may be it will run faster than this method.