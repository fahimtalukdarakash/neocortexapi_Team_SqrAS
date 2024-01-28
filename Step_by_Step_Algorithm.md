**Step 1:** Creating a Dictonary for 100 input values.</br>
**Step 2:** First check whether SDR of all inputs present or not.</br>
**Step 2.1:** If SDR of all inputs exits and save to SDR of inputs to the Dictionary.</br>
**Step 2.2:**  If SDR of all inputs don't exits then don't save the SDR of inputs to the Dictionary and check whether all SDR of inputs present or not.</br>
**Step 3:** When all the SDR of input present and check whether an input has minimum 2 arrays of SDR or not.</br>
**Step 4:** After getting the SDR of all inputs and minimum array count is 2 for all the inputs then comparing of the SDR of each cycles start.</br>
**Step 5:** Comparing Last 2 cycles of SDR of all inputs.</br>
**Step 6:** If consecutive "N" cycles of SDR of all inputs are same then we will break the loop.</br>
**Step 7:** If consecutive "N" cycles or there is one cycle of SDR is changed then "counting for cycle" will become 0 again and from the next iteration calculate the "counting for cycle" again and run the program until it is same as "N".</br>