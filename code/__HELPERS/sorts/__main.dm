	//These are macros used to reduce on proc calls
#define fetchElement(L, i) (associative) ? L[L[i]] : L[i]

	//Minimum sized sequence that will be merged. Anything smaller than this will use binary-insertion sort.
	//Should be a power of 2
#define MIN_MERGE 32

	//When we get into galloping mode, we stay there until both runs win less often than MIN_GALLOP consecutive times.
#define MIN_GALLOP 7

	//This is a global instance to allow much of this code to be reused. The interfaces are kept separately
GLOBAL_DATUM_INIT(sortInstance, /datum/sortInstance, new())
/datum/sortInstance
	//The array being sorted.
	var/list/L

	//The comparator proc-reference
	var/cmp = /proc/cmp_numeric_asc

	//whether we are sorting list keys (0: L[i]) or associated values (1: L[L[i]])
	var/associative = 0

	//This controls when we get *into* galloping mode.  It is initialized	to MIN_GALLOP.
	//The mergeLo and mergeHi methods nudge it higher for random data, and lower for highly structured data.
	var/minGallop = MIN_GALLOP

	//Stores information regarding runs yet to be merged.
	//Run i starts at runBase[i] and extends for runLen[i] elements.
	//runBase[i] + runLen[i] == runBase[i+1]
	var/list/runBases = list()
	var/list/runLens = list()


/datum/sortInstance/proc/timSort(start, end)
	runBases.Cut()
	runLens.Cut()

	var/remaining = end - start

	//If array is small, do a 'mini-TimSort' with no merges
	if(remaining < MIN_MERGE)
		var/initRunLen = countRunAndMakeAscending(start, end)
		binarySort(start, end, start+initRunLen)
		return

	//March over the array finding natural runs
	//Extend any short natural runs to runs of length minRun
	var/minRun = minRunLength(remaining)

	do
			//identify next run
		var/runLen = countRunAndMakeAscending(start, end)

			//if run is short, extend to min(minRun, remaining)
		if(runLen < minRun)
			var/force = (remaining <= minRun) ? remaining : minRun

			binarySort(start, start+force, start+runLen)
			runLen = force

			//add data about run to queue
		runBases.Add(start)
		runLens.Add(runLen)

			//maybe merge
		mergeCollapse()

			//Advance to find next run
		start += runLen
		remaining -= runLen

	while(remaining > 0)


		//Merge all remaining runs to complete sort
	//ASSERT(start == end)
	mergeForceCollapse();
	//ASSERT(runBases.len == 1)

		//reset minGallop, for successive calls
	minGallop = MIN_GALLOP

	return L

	/*
	Sorts the specified portion of the specified array using a binary
	insertion sort.  This is the best method for sorting small numbers
	of elements.  It requires O(n log n) compares, but O(n^2) data
	movement (worst case).

	If the initial part of the specified range is already sorted,
	this method can take advantage of it: the method assumes that the
	elements in range [lo,start) are already sorted

	lo		the index of the first element in the range to be sorted
	hi		the index after the last element in the range to be sorted
	start	the index of the first element in the range that is	not already known to be sorted
	*/
/datum/sortInstance/proc/binarySort(lo, hi, start)
	//ASSERT(lo <= start && start <= hi)
	if(start <= lo)
		start = lo + 1

	for(,start < hi, ++start)
		var/pivot = fetchElement(L,start)

		//set left and right to the index where pivot belongs
		var/left = lo
		var/right = start
		//ASSERT(left <= right)

		//[lo, left) elements <= pivot < [right, start) elements
		//in other words, find where the pivot element should go using bisection search
		while(left < right)
			var/mid = (left + right) >> 1	//round((left+right)/2)
			if(call(cmp)(fetchElement(L,mid), pivot) > 0)
				right = mid
			else
				left = mid+1

		//ASSERT(left == right)
		moveElement(L, start, left)	//move pivot element to correct location in the sorted range

	/*
	Returns the length of the run beginning at the specified position and reverses the run if it is back-to-front

	A run is the longest ascending sequence with:
		a[lo] <= a[lo + 1] <= a[lo + 2] <= ...
	or the longest descending sequence with:
		a[lo] >  a[lo + 1] >  a[lo + 2] >  ...

	For its intended use in a stable mergesort, the strictness of the
	definition of "descending" is needed so that the call can safely
	reverse a descending sequence without violating stability.
	*/
/datum/sortInstance/proc/countRunAndMakeAscending(lo, hi)
	//ASSERT(lo < hi)

	var/runHi = lo + 1
	if(runHi >= hi)
		return 1

	var/last = fetchElement(L,lo)
	var/current = fetchElement(L,runHi++)

	if(call(cmp)(current, last) < 0)
		while(runHi < hi)
			last = current
			current = fetchElement(L,runHi)
			if(call(cmp)(current, last) >= 0)
				break
			++runHi
		reverseRange(L, lo, runHi)
	else
		while(runHi < hi)
			last = current
			current = fetchElement(L,runHi)
			if(call(cmp)(current, last) < 0)
				break
			++runHi

	return runHi - lo

	//Returns the minimum acceptable run length for an array of the specified length.
	//Natural runs shorter than this will be extended with binarySort
/datum/sortInstance/proc/minRunLength(n)
	//ASSERT(n >= 0)
	var/r = 0	//becomes 1 if any bits are shifted off
	while(n >= MIN_MERGE)
		r |= (n & 1)
		n >>= 1
	return n + r

	//Examines the stack of runs waiting to be merged and merges adjacent runs until the stack invariants are reestablished:
	//	runLen[i-3] > runLen[i-2] + runLen[i-1]
	//	runLen[i-2] > runLen[i-1]
	//This method is called each time a new run is pushed onto the stack.
	//So the invariants are guaranteed to hold for i<stackSize upon entry to the method
/datum/sortInstance/proc/mergeCollapse()
	while(runBases.len >= 2)
		var/n = runBases.len - 1
		if(n > 1 && runLens[n-1] <= runLens[n] + runLens[n+1])
			if(runLens[n-1] < runLens[n+1])
				--n
			mergeAt(n)
		else if(runLens[n] <= runLens[n+1])
			mergeAt(n)
		else
			break	//Invariant is established


	//Merges all runs on the stack until only one remains.
	//Called only once, to finalise the sort
/datum/sortInstance/proc/mergeForceCollapse()
	while(runBases.len >= 2)
		var/n = runBases.len - 1
		if(n > 1 && runLens[n-1] < runLens[n+1])
			--n
		mergeAt(n)


	//Merges the two consecutive runs at stack indices i and i+1
	//Run i must be the penultimate or antepenultimate run on the stack
	//In other words, i must be equal to stackSize-2 or stackSize-3
/datum/sortInstance/proc/mergeAt(i)
	//ASSERT(runBases.len >= 2)
	//ASSERT(i >= 1)
	//ASSERT(i == runBases.len - 1 || i == runBases.len - 2)

	var/base1 = runBases[i]
	var/base2 = runBases[i+1]
	var/len1 = runLens[i]
	var/len2 = runLens[i+1]

	//ASSERT(len1 > 0 && len2 > 0)
	//ASSERT(base1 + len1 == base2)

	//Record the legth of the combined runs. If i is the 3rd last run now, also slide over the last run
	//(which isn't involved in this merge). The current run (i+1) goes away in any case.
	runLens[i] += runLens[i+1]
	runLens.Cut(i+1, i+2)
	runBases.Cut(i+1, i+2)


	//Find where the first element of run2 goes in run1.
	//Prior elements in run1 can be ignored (because they're already in place)
	var/k = gallopRight(fetchElement(L,base2), base1, len1, 0)
	//ASSERT(k >= 0)
	base1 += k
	len1 -= k
	if(len1 == 0)
		return

	//Find where the last element of run1 goes in run2.
	//Subsequent elements in run2 can be ignored (because they're already in place)
	len2 = gallopLeft(fetchElement(L,base1 + len1 - 1), base2, len2, len2-1)
	//ASSERT(len2 >= 0)
	if(len2 == 0)
		return

	//Merge remaining runs, using tmp array with min(len1, len2) elements
	if(len1 <= len2)
		mergeLo(base1, len1, base2, len2)
	else
		mergeHi(base1, len1, base2, len2)


	/*
		Locates the position to insert key within the specified sorted range
		If the range contains elements equal to key, this will return the index of the LEFTMOST of those elements

		key		the element to be inserted into the sorted range
		base	the index of the first element of the sorted range
		len		the length of the sorted range, must be greater than 0
		hint	the offset from base at which to begin the search, such that 0 <= hint < len; i.e. base <= hint < base+hint

		Returns the index at which to insert element 'key'
	*/
/datum/sortInstance/proc/gallopLeft(key, base, len, hint)
	//ASSERT(len > 0 && hint >= 0 && hint < len)

	var/lastOffset = 0
	var/offset = 1
	if(call(cmp)(key, fetchElement(L,base+hint)) > 0)
		var/maxOffset = len - hint
		while(offset < maxOffset && call(cmp)(key, fetchElement(L,base+hint+offset)) > 0)
			lastOffset = offset
			offset = (offset << 1) + 1

		if(offset > maxOffset)
			offset = maxOffset

		lastOffset += hint
		offset += hint

	else
		var/maxOffset = hint + 1
		while(offset < maxOffset && call(cmp)(key, fetchElement(L,base+hint-offset)) <= 0)
			lastOffset = offset
			offset = (offset << 1) + 1

		if(offset > maxOffset)
			offset = maxOffset

		var/temp = lastOffset
		lastOffset = hint - offset
		offset = hint - temp

		//ASSERT(-1 <= lastOffset && lastOffset < offset && offset <= len)

	//Now L[base+lastOffset] < key <= L[base+offset], so key belongs somewhere to the right of lastOffset but no farther than
	//offset. Do a binary search with invariant L[base+lastOffset-1] < key <= L[base+offset]
	++lastOffset
	while(lastOffset < offset)
		var/m = lastOffset + ((offset - lastOffset) >> 1)

		if(call(cmp)(key, fetchElement(L,base+m)) > 0)
			lastOffset = m + 1
		else
			offset = m

	//ASSERT(lastOffset == offset)
	return offset

	/**
	 * Like gallopLeft, except that if the range contains an element equal to
	 * key, gallopRight returns the index after the rightmost equal element.
	 *
	 * @param key the key whose insertion point to search for
	 * @param a the array in which to search
	 * @param base the index of the first element in the range
	 * @param len the length of the range; must be > 0
	 * @param hint the index at which to begin the search, 0 <= hint < n.
	 *	 The closer hint is to the result, the faster this method will run.
	 * @param c the comparator used to order the range, and to search
	 * @return the int k,  0 <= k <= n such that a[b + k - 1] <= key < a[b + k]
	 */
/datum/sortInstance/proc/gallopRight(key, base, len, hint)
	//ASSERT(len > 0 && hint >= 0 && hint < len)

	var/offset = 1
	var/lastOffset = 0
	if(call(cmp)(key, fetchElement(L,base+hint)) < 0)	//key <= L[base+hint]
		var/maxOffset = hint + 1	//therefore we want to insert somewhere in the range [base,base+hint] = [base+,base+(hint+1))
		while(offset < maxOffset && call(cmp)(key, fetchElement(L,base+hint-offset)) < 0)	//we are iterating backwards
			lastOffset = offset
			offset = (offset << 1) + 1	//1 3 7 15

		if(offset > maxOffset)
			offset = maxOffset

		var/temp = lastOffset
		lastOffset = hint - offset
		offset = hint - temp

	else	//key > L[base+hint]
		var/maxOffset = len - hint	//therefore we want to insert somewhere in the range (base+hint,base+len) = [base+hint+1, base+hint+(len-hint))
		while(offset < maxOffset && call(cmp)(key, fetchElement(L,base+hint+offset)) >= 0)
			lastOffset = offset
			offset = (offset << 1) + 1

		if(offset > maxOffset)
			offset = maxOffset

		lastOffset += hint
		offset += hint

	//ASSERT(-1 <= lastOffset && lastOffset < offset && offset <= len)

	++lastOffset
	while(lastOffset < offset)
		var/m = lastOffset + ((offset - lastOffset) >> 1)

		if(call(cmp)(key, fetchElement(L,base+m)) < 0)	//key <= L[base+m]
			offset = m
		else							//key > L[base+m]
			lastOffset = m + 1

	//ASSERT(lastOffset == offset)

	return offset


	//Merges two adjacent runs in-place in a stable fashion.
	//For performance this method should only be called when len1 <= len2!
/datum/sortInstance/proc/mergeLo(base1, len1, base2, len2)
	//ASSERT(len1 > 0 && len2 > 0 && base1 + len1 == base2)

	var/cursor1 = base1
	var/cursor2 = base2

	//degenerate cases
	if(len2 == 1)
		moveElement(L, cursor2, cursor1)
		return

	if(len1 == 1)
		moveElement(L, cursor1, cursor2+len2)
		return


	//Move first element of second run
	moveElement(L, cursor2++, cursor1++)
	--len2

	outer:
		while(1)
			var/count1 = 0	//# of times in a row that first run won
			var/count2 = 0	//	"	"	"	"	"	"  second run won

			//do the straightfoward thin until one run starts winning consistently

			do
				//ASSERT(len1 > 1 && len2 > 0)
				if(call(cmp)(fetchElement(L,cursor2), fetchElement(L,cursor1)) < 0)
					moveElement(L, cursor2++, cursor1++)
					--len2

					++count2
					count1 = 0

					if(len2 == 0)
						break outer
				else
					++cursor1

					++count1
					count2 = 0

					if(--len1 == 1)
						break outer

			while((count1 | count2) < minGallop)


			//one run is winning consistently so galloping may provide huge benifits
			//so try galloping, until such time as the run is no longer consistently winning
			do
				//ASSERT(len1 > 1 && len2 > 0)

				count1 = gallopRight(fetchElement(L,cursor2), cursor1, len1, 0)
				if(count1)
					cursor1 += count1
					len1 -= count1

					if(len1 <= 1)
						break outer

				moveElement(L, cursor2, cursor1)
				++cursor2
				++cursor1
				if(--len2 == 0)
					break outer

				count2 = gallopLeft(fetchElement(L,cursor1), cursor2, len2, 0)
				if(count2)
					moveRange(L, cursor2, cursor1, count2)

					cursor2 += count2
					cursor1 += count2
					len2 -= count2

					if(len2 == 0)
						break outer

				++cursor1
				if(--len1 == 1)
					break outer

				--minGallop

			while((count1|count2) > MIN_GALLOP)

			if(minGallop < 0)
				minGallop = 0
			minGallop += 2;  // Penalize for leaving gallop mode


	if(len1 == 1)
		//ASSERT(len2 > 0)
		moveElement(L, cursor1, cursor2+len2)

	//else
		//ASSERT(len2 == 0)
		//ASSERT(len1 > 1)


/datum/sortInstance/proc/mergeHi(base1, len1, base2, len2)
	//ASSERT(len1 > 0 && len2 > 0 && base1 + len1 == base2)

	var/cursor1 = base1 + len1 - 1	//start at end of sublists
	var/cursor2 = base2 + len2 - 1

	//degenerate cases
	if(len2 == 1)
		moveElement(L, base2, base1)
		return

	if(len1 == 1)
		moveElement(L, base1, cursor2+1)
		return

	moveElement(L, cursor1--, cursor2-- + 1)
	--len1

	outer:
		while(1)
			var/count1 = 0	//# of times in a row that first run won
			var/count2 = 0	//	"	"	"	"	"	"  second run won

			//do the straightfoward thing until one run starts winning consistently
			do
				//ASSERT(len1 > 0 && len2 > 1)
				if(call(cmp)(fetchElement(L,cursor2), fetchElement(L,cursor1)) < 0)
					moveElement(L, cursor1--, cursor2-- + 1)
					--len1

					++count1
					count2 = 0

					if(len1 == 0)
						break outer
				else
					--cursor2
					--len2

					++count2
					count1 = 0

					if(len2 == 1)
						break outer
			while((count1 | count2) < minGallop)

			//one run is winning consistently so galloping may provide huge benifits
			//so try galloping, until such time as the run is no longer consistently winning
			do
				//ASSERT(len1 > 0 && len2 > 1)

				count1 = len1 - gallopRight(fetchElement(L,cursor2), base1, len1, len1-1)	//should cursor1 be base1?
				if(count1)
					cursor1 -= count1

					moveRange(L, cursor1+1, cursor2+1, count1)	//cursor1+1 == cursor2 by definition

					cursor2 -= count1
					len1 -= count1

					if(len1 == 0)
						break outer

				--cursor2

				if(--len2 == 1)
					break outer

				count2 = len2 - gallopLeft(fetchElement(L,cursor1), cursor1+1, len2, len2-1)
				if(count2)
					cursor2 -= count2
					len2 -= count2

					if(len2 <= 1)
						break outer

				moveElement(L, cursor1--, cursor2-- + 1)
				--len1

				if(len1 == 0)
					break outer

				--minGallop
			while((count1|count2) > MIN_GALLOP)

			if(minGallop < 0)
				minGallop = 0
			minGallop += 2	// Penalize for leaving gallop mode

	if(len2 == 1)
		//ASSERT(len1 > 0)

		cursor1 -= len1
		moveRange(L, cursor1+1, cursor2+1, len1)

	//else
		//ASSERT(len1 == 0)
		//ASSERT(len2 > 0)


/datum/sortInstance/proc/mergeSort(start, end)
	var/remaining = end - start

	//If array is small, do an insertion sort
	if(remaining < MIN_MERGE)
		binarySort(start, end, start/*+initRunLen*/)
		return

	var/minRun = minRunLength(remaining)

	do
		var/runLen = (remaining <= minRun) ? remaining : minRun

		binarySort(start, start+runLen, start)

		//add data about run to queue
		runBases.Add(start)
		runLens.Add(runLen)

		//Advance to find next run
		start += runLen
		remaining -= runLen

	while(remaining > 0)

	while(runBases.len >= 2)
		var/n = runBases.len - 1
		if(n > 1 && runLens[n-1] <= runLens[n] + runLens[n+1])
			if(runLens[n-1] < runLens[n+1])
				--n
			mergeAt2(n)
		else if(runLens[n] <= runLens[n+1])
			mergeAt2(n)
		else
			break	//Invariant is established

	while(runBases.len >= 2)
		var/n = runBases.len - 1
		if(n > 1 && runLens[n-1] < runLens[n+1])
			--n
		mergeAt2(n)

	return L

/datum/sortInstance/proc/mergeAt2(i)
	var/cursor1 = runBases[i]
	var/cursor2 = runBases[i+1]

	var/end1 = cursor1+runLens[i]
	var/end2 = cursor2+runLens[i+1]

	var/val1 = fetchElement(L,cursor1)
	var/val2 = fetchElement(L,cursor2)

	while(1)
		if(call(cmp)(val1,val2) <= 0)
			if(++cursor1 >= end1)
				break
			val1 = fetchElement(L,cursor1)
		else
			moveElement(L,cursor2,cursor1)

			if(++cursor2 >= end2)
				break
			++end1
			++cursor1

			val2 = fetchElement(L,cursor2)


	//Record the legth of the combined runs. If i is the 3rd last run now, also slide over the last run
	//(which isn't involved in this merge). The current run (i+1) goes away in any case.
	runLens[i] += runLens[i+1]
	runLens.Cut(i+1, i+2)
	runBases.Cut(i+1, i+2)

#undef MIN_GALLOP
#undef MIN_MERGE

#undef fetchElement
