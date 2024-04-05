[![license](https://img.shields.io/github/license/mashape/apistatus.svg?maxAge=2592000)](https://github.com/ddobric/htmdotnet/blob/master/LICENSE)
[![buildStatus](https://github.com/ddobric/neocortexapi/workflows/.NET%20Core/badge.svg)](https://github.com/ddobric/neocortexapi/actions?query=workflow%3A%22.NET+Core%22)
# ML 23/24-03 Implement the new Spatial Learning experiment

Forked from: [neocortexapi] (https://github.com/ddobric/neocortexapi)
[Main Branch](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS)

Implementation of New Spatial Pattern Learning : [Spatial Pattern Learning](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs) </br>
Code Added: [172-236 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/4a61b7bf2ddfb28020ac9c751740fc19c28b38df/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs#L172C13-L236C14) </br>
Code Added: [256-263 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/4a61b7bf2ddfb28020ac9c751740fc19c28b38df/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs#L256C20-L263C132) </br>
Code Added: [265-283 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/4a61b7bf2ddfb28020ac9c751740fc19c28b38df/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs#L265C20-L283C22) </br>
Code Added: [285-301 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/4a61b7bf2ddfb28020ac9c751740fc19c28b38df/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs#L285C20-L301C22) </br>
Code Added: [307-382 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs#L307C14-L382C63) </br>
Code Added: [400-708 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/4a61b7bf2ddfb28020ac9c751740fc19c28b38df/source/Samples/NeoCortexApiSample/SpatialPatternLearning.cs#L400) </br>

Applying Boosting Algorithm in Spatial Pooler Algorithm: [Boosting of Columns with Low Overlap](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/source/NeoCortexApi/SpatialPooler.cs)</br>
Code added: [680-722 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/aa74a4ef6155784d7d0abc14669e817eab47d111/source/NeoCortexApi/SpatialPooler.cs#L680C1-L723C1)</br>

Code added: [1304-1322 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/4a61b7bf2ddfb28020ac9c751740fc19c28b38df/source/NeoCortexApi/SpatialPooler.cs#L1304C1-L1322C10) </br>

Applying Stringify Function in Helpers.cs: </br>
Code added: [259-285 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/source/NeoCortexApi/Helpers.cs#L259C1-L285C10)

Creating the Function of Getting Connected Input Bits for a Column in Column.cs: </br>
Code Added: [325-330 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/source/NeoCortexEntities/Entities/Column.cs#L325C5-L330C10)

Creating a Function of Draw Bit Maps for connected Input Bits in NeoCortexUtils.cs: </br>
Code Added:[129-171 lines](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/source/NeoCortexUtils/NeoCortexUtils.cs#L129C2-L171C14)

# Abstract
Spatial pattern learning, particularly in the context of algorithms like the Spatial Pooler in Hierarchical Temporal Memory (HTM) models, refers to the process of learning patterns of activation in a spatially distributed manner. In HTM theory, the Spatial Pooler is a crucial component responsible for learning spatial patterns in the input data. It receives sparse binary input signals (often encoded from sensory data) and learns to create a sparse distributed representation (SDR) of the input in a high-dimensional space. Spatial pattern learning is valuable for a wide range of applications, including sensory data processing, anomaly detection, pattern recognition, and predictive modeling. Its efficiency, robustness, and adaptability make it a powerful tool for building intelligent systems that can learn from and interact with complex real-world environments. The previous experiment of Spatial Pattern Learning wasn’t efficient as it didn’t give the SDR (mini columns) of inputs 51 to 99 for the first 40 cycles and ran for 1000 iterations which isn’t time efficient. This new Spatial Pattern Learning, at first, it will resolve the issue of not giving the SDR for inputs 51 to 99, then it will break the iterations by matching some criteria as well as give statistical information and better representation of SDRs to show how it changes in each cycle of Spatial Pattern Learning.

# Introduction
Spatial Pattern Learning using Sparse Distributed Representations (SDRs) represents a pivotal area within the realm of artificial intelligence, drawing inspiration from the brain's remarkable ability to process and recognize spatial patterns which is based on the fundamentals of hierarchical temporal memory (HTM). In HTM, Sparse Distributed Representations are crucial data structures, posing a central challenge in their construction and management. The Spatial Pooler Algorithm transforms a sequence of input bits into the SDR representation. To understand this representation, these SDRs are influenced by the structure of the human brain neocortex, specifically the activated columns on the brain membrane surface. In essence, the algorithm helps convert information into a format inspired by the brain's architecture, contributing to the study of intelligence in artificial systems.

# Methodology

### Analyzing the Code of Spatial Pattern Learning Experiment
The implemented SpatialPatternLearning algorithm begins by initializing HtmConfig and Encoder values. Random input values are then generated, and the spatial pooler algorithm is applied to obtain a column list for each input. The algorithm utilizes a HomeostaticPlasticityController to manage the learning process, initially setting the spatial pooler in a new-born stage where boosting is active but instability exists. After this stage, the HomeostaticPlasticityController controls the learning process, aiming to stabilize the generated sparse distributed representations (SDRs). The input encoding and column activation are handled by layers added sequentially: Encoder and Spatial Pooler. During 1000 iterations of learning, each input is encoded and processed by the spatial pooler, with the first 40 iterations focusing on stabilizing the column lists. The HomeostaticPlasticityController adjusts column activations, aiming for stability or boosting as needed. Stability is achieved when 97% similarity in column lists is maintained for 50 consecutive cycles. Finally, the program checks for stability over five cycles before terminating.

Detail Description : [Documentation](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/Documentation%20Folder/Project%20Documentation.md#task-1-analyzing-the-code-of-spatial-pattern-learning)

### Investegating the reason, why first 40 cycles don't give mini-columns list of SDR for inputs 51 to 99.

After investigating throughly, found the problem in Spatial Pooler algorithm. In the Spatial Pooler Algorithm, there was a function called `BoostColsWithLowOverlap`. This function is responsible for boosting the columns with low overlap. So, the solution of this problem is to reduce the weak columns as fast as possible.
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
Detail Description : [Documentation](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/Documentation%20Folder/Project%20Documentation.md#task-2-investegating-the-reason-why-first-40-cycles-dont-give-mini-columns-list-of-sdr-for-inputs-51-to-99)

### Implementing new spatial pattern learning experiment

After achieving stability, indicated by the variable isInStableState being true, the column lists for each input are stored in a dictionary, indexed by input and cycle number. This storage includes column lists for each input at cycles 384 and 383. Following stability, the algorithm compares column lists for another 100 cycles to detect any changes. If isInStableState turns false again within this period, the algorithm waits for it to return to true over 50 cycles. During this waiting period, the SDR arrays for each input cycle are cleared, and initial values are reset. Once isInStableState is true and SDR arrays are stored, the algorithm compares the last two cycles' SDR arrays for each input. This comparison is repeated for 100 consecutive cycles, resetting the countForCycle variable if mismatches occur. Upon meeting the condition for 100 consecutive matching cycles, the loop terminates.

Detail Description: [Documentation](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/Documentation%20Folder/Project%20Documentation.md#task-3-implementing-new-spatial-pattern-learning-experiment)

### Statistical information regarding Spatial Pattern Learning experiment
In the Spatial Pattern Learning experiment, during the execution of the program some statistical information has been extracted and visualised for further understaning. Here is the list of information shown in the new experiment:
- Info-1: Checking which input is getting stable at which cycle
- Info-2: Checking the percentage of stable input in every cycle
- Info-3: Generating Bitmaps to see how the SDRs is changing in every cycle for each input
- Info-4: Which column will be activated for which inputs during the experiment 
- Info-5: Generating bit maps for a column that is connected with the input bits
- Info-6: **Parameter Tuning** which shows the SDR and cycles stability for different tuned parameter

# Result

After successfully implementing the experiment, this program shows the exceted output in different manner. Firstly, write out the dictionary for the stable cycles outputs.

### Write out the dictionary at the end of the experiment
After breaking the loop, printing the dictionary at the end.
Detail Description: [Documentation](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/Documentation%20Folder/Project%20Documentation.md#task-4-write-out-the-dictionary-at-the-end)

### Statistical info about the stability of all mini-columns

Initially, the output is presented as outlined in the project description. Following each cycle, the algorithm indicates whether an input has stabilized and, if so, at which cycle it achieved stability. Additionally, after each cycle, the percentage of stable inputs for that specific cycle is displayed. Bitmaps are generated for each input at every cycle, facilitating a visual understanding of how the SDRs change per input cycle, showing a column will be activated for which inputs, generated bitmaps for showing a column is connected with which input bits.

- Statistical Informations Details Description: [Documentation](https://github.com/fahimtalukdarakash/neocortexapi_Team_SqrAS/blob/master/Documentation%20Folder/Project%20Documentation.md#task-5-providing-some-more-readable-statistical-info-about-the-stability-of-all-mini-columns)


# Conclusion

The `SpatialPatternLearning` experiment has provided invaluable insights into the capabilities and dynamics of spatial pattern learning within Spatial pooler. Through meticulous experimentation and analysis, demonstrated the efficacy of the program. Our project was to implement new spatial pattern learning. At first, investigated why first 40 cycles don't give mini columns list for inputs 51 to input 99. This is happening because of the boosting of low overlap columns. There increasing of overlap duty cycles value and decreasing the value of minimum overlap cycles solved the problem. After this, the main task of the program was, it should exit from the loop by a certain condition after the variable `isInStableState` is set to true which implemented successfully in the program. It ensures that after the variable `isInStableState` is true, then it's stability will not change by checking consecutive 100 cycles. At the end, the dictionary is written.  It provides, how many iterations the SDR for the input was not changes. After `isInStableState` is true, counting and showing how many cycles are stable. After each cycle, whether a input is stable or not and if a input is stable then at which it gets stable as well as showing by percentage that how many inputs are stable for that cycle. Generating bitmaps for each input of every cycle to represent how actually the SDRs are changing through cycles for each input. Provided other statistical information, for example changing the parameters, how the program effects in stability as well as at which cycle all the input have mini column list by showing graphs.
