using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DbMigrations.Client.Infrastructure.Implementation;

namespace DbMigrations.Client.Infrastructure 
{
    public enum Operation
    {
        Delete, Insert, Equal
    }

    public struct Diff
    {
        internal static Diff Create(Operation operation, string text, int? lineNumber = null)
        {
            return new Diff(operation, text, lineNumber);
        }

        internal static Diff Equal(string text)
        {
            return Create(Operation.Equal, text);
        }

        internal static Diff Insert(string text)
        {
            return Create(Operation.Insert, text);
        }
        internal static Diff Delete(string text)
        {
            return Create(Operation.Delete, text);
        }

        public readonly Operation Operation;
        public readonly string Text;
        public readonly int? LineNumber;

        Diff(Operation operation, string text, int? lineNumber = null)
        {
            // Construct a diff with the specified operation and text.
            Operation = operation;
            Text = text;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Generate a human-readable version of this Diff.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var prettyText = Text.Replace('\n', '\u00b6');
            return "Diff(" + Operation + ",\"" + prettyText + "\")";
        }

        /// <summary>
        /// Is this Diff equivalent to another Diff?
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(Object obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            return Equals((Diff) obj, this);
        }

        public bool Equals(Diff obj)
        {
            return obj.Operation == Operation && obj.Text == Text;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode() ^ Operation.GetHashCode();
        }

        internal Diff Replace(string text, int? lineNumber = null)
        {
            return Create(Operation, text, lineNumber ?? LineNumber);
        }

        internal Diff Copy()
        {
            return Create(Operation, Text, LineNumber);
        }

        /// <summary>
        /// Find the differences between two texts.
        /// </summary>
        /// <param name="text1">Old string to be diffed</param>
        /// <param name="text2">New string to be diffed</param>
        /// <param name="timeoutInSeconds">if specified, certain optimizations may be enabled to meet the time constraint, possibly resulting in a less optimal diff</param>
        /// <param name="checklines">If false, then don't run a line-level diff first to identify the changed areas. If true, then run a faster slightly less optimal diff.</param>
        /// <returns></returns>
        public static List<Diff> Compute(string text1, string text2, float timeoutInSeconds = 0f, bool checklines = true)
        {
            CancellationTokenSource cts;
            if (timeoutInSeconds <= 0)
            {
                cts = new CancellationTokenSource();
            }
            else
            {
                var waitTime = TimeSpan.FromSeconds(timeoutInSeconds);
                cts = new CancellationTokenSource(waitTime);
            }

            return Compute(text1, text2, checklines, cts.Token, timeoutInSeconds > 0);
        }

        public static List<Diff> Compute(string text1, string text2, bool checkLines, CancellationToken token, bool optimizeForSpeed)
        {
            return DiffAlgorithm.Compute(text1, text2, checkLines, token, optimizeForSpeed);
        }


    }


#region Implementation
    namespace Implementation
    {
        static class DiffAlgorithm
        {

            /// <summary>
            /// Find the differences between two texts.  Simplifies the problem by
            /// stripping any common prefix or suffix off the texts before diffing.
            /// </summary>
            /// <param name="text1">Old string to be diffed.</param>
            /// <param name="text2">New string to be diffed.</param>
            /// <param name="checklines">Speedup flag.  If false, then don't run a line-level diff first to identify the changed areas. If true, then run a faster slightly less optimal diff.</param>
            /// <param name="token">Cancellation token for cooperative cancellation</param>
            /// <param name="optimizeForSpeed">Should optimizations be enabled?</param>
            /// <returns></returns>
            public static List<Diff> Compute(string text1, string text2, bool checklines, CancellationToken token, bool optimizeForSpeed)
            {
                // Check for null inputs not needed since null can't be passed in C#.

                // Check for equality (speedup).
                List<Diff> diffs;
                if (text1 == text2)
                {
                    diffs = new List<Diff>();
                    if (text1.Length != 0)
                    {
                        diffs.Add(Diff.Equal(text1));
                    }
                    return diffs;
                }

                // Trim off common prefix (speedup).
                var commonlength = TextUtil.CommonPrefix(text1, text2);
                var commonprefix = text1.Substring(0, commonlength);
                text1 = text1.Substring(commonlength);
                text2 = text2.Substring(commonlength);

                // Trim off common suffix (speedup).
                commonlength = TextUtil.CommonSuffix(text1, text2);
                var commonsuffix = text1.Substring(text1.Length - commonlength);
                text1 = text1.Substring(0, text1.Length - commonlength);
                text2 = text2.Substring(0, text2.Length - commonlength);

                // Compute the diff on the middle block.
                diffs = ComputeImpl(text1, text2, checklines, token, optimizeForSpeed);

                // Restore the prefix and suffix.
                if (commonprefix.Length != 0)
                {
                    diffs.Insert(0, Diff.Equal(commonprefix));
                }
                if (commonsuffix.Length != 0)
                {
                    diffs.Add(Diff.Equal(commonsuffix));
                }

                diffs.CleanupMerge();
                return diffs;
            }

            /// <summary>
            /// Find the differences between two texts.  Assumes that the texts do not
            /// have any common prefix or suffix.
            /// </summary>
            /// <param name="text1">Old string to be diffed.</param>
            /// <param name="text2">New string to be diffed.</param>
            /// <param name="checklines">Speedup flag.  If false, then don't run a line-level diff first to identify the changed areas. If true, then run a faster slightly less optimal diff.</param>
            /// <param name="token">Cancellation token for cooperative cancellation</param>
            /// <param name="optimizeForSpeed">Should optimizations be enabled?</param>
            /// <returns></returns>
            private static List<Diff> ComputeImpl(
                string text1,
                string text2,
                bool checklines, CancellationToken token, bool optimizeForSpeed)
            {
                var diffs = new List<Diff>();

                if (text1.Length == 0)
                {
                    // Just add some text (speedup).
                    diffs.Add(Diff.Insert(text2));
                    return diffs;
                }

                if (text2.Length == 0)
                {
                    // Just delete some text (speedup).
                    diffs.Add(Diff.Delete(text1));
                    return diffs;
                }

                var longtext = text1.Length > text2.Length ? text1 : text2;
                var shorttext = text1.Length > text2.Length ? text2 : text1;
                var i = longtext.IndexOf(shorttext, StringComparison.Ordinal);
                if (i != -1)
                {
                    // Shorter text is inside the longer text (speedup).
                    var op = text1.Length > text2.Length ? Operation.Delete : Operation.Insert;
                    diffs.Add(Diff.Create(op, longtext.Substring(0, i)));
                    diffs.Add(Diff.Equal(shorttext));
                    diffs.Add(Diff.Create(op, longtext.Substring(i + shorttext.Length)));
                    return diffs;
                }

                if (shorttext.Length == 1)
                {
                    // Single character string.
                    // After the previous speedup, the character can't be an equality.
                    diffs.Add(Diff.Delete(text1));
                    diffs.Add(Diff.Insert(text2));
                    return diffs;
                }

                // Don't risk returning a non-optimal diff if we have unlimited time.
                if (optimizeForSpeed)
                {
                    // Check to see if the problem can be split in two.
                    var result = TextUtil.HalfMatch(text1, text2);
                    if (!result.IsEmpty)
                    {
                        // A half-match was found, sort out the return data.
                        // Send both pairs off for separate processing.
                        var diffsA = Compute(result.Prefix1, result.Prefix2, checklines, token, optimizeForSpeed);
                        var diffsB = Compute(result.Suffix1, result.Suffix2, checklines, token, optimizeForSpeed);

                        // Merge the results.
                        diffs = diffsA;
                        diffs.Add(Diff.Equal(result.CommonMiddle));
                        diffs.AddRange(diffsB);
                        return diffs;
                    }
                }
                if (checklines && text1.Length > 100 && text2.Length > 100)
                {
                    return LineDiff(text1, text2, token, optimizeForSpeed);
                }

                return MyersDiffBisect(text1, text2, token, optimizeForSpeed);
            }

            /// <summary>
            /// Do a quick line-level diff on both strings, then rediff the parts for
            /// greater accuracy. This speedup can produce non-minimal Diffs.
            /// </summary>
            /// <param name="text1"></param>
            /// <param name="text2"></param>
            /// <param name="token"></param>
            /// <param name="optimizeForSpeed"></param>
            /// <returns></returns>
            private static List<Diff> LineDiff(string text1, string text2, CancellationToken token, bool optimizeForSpeed)
            {
                // Scan the text on a line-by-line basis first.
                var compressor = new LineToCharCompressor();
                text1 = compressor.Compress(text1);
                text2 = compressor.Compress(text2);
                var lineDiffs = Compute(text1, text2, false, token, optimizeForSpeed);
                var diffs = (
                    from diff in lineDiffs
                    let decompress = compressor.Decompress(diff.Text)
                    let lineNumber = diff.Text[0]
                    select diff.Replace(decompress, lineNumber)
                    ).ToList();

                // Eliminate freak matches (e.g. blank lines)
                diffs.CleanupSemantic();

                // Rediff any replacement blocks, this time character-by-character.
                // Add a dummy entry at the end.
                diffs.Add(Diff.Equal(string.Empty));
                var pointer = 0;
                var countDelete = 0;
                var countInsert = 0;
                var textDelete = string.Empty;
                var textInsert = string.Empty;
                while (pointer < diffs.Count)
                {
                    switch (diffs[pointer].Operation)
                    {
                        case Operation.Insert:
                            countInsert++;
                            textInsert += diffs[pointer].Text;
                            break;
                        case Operation.Delete:
                            countDelete++;
                            textDelete += diffs[pointer].Text;
                            break;
                        case Operation.Equal:
                            // Upon reaching an equality, check for prior redundancies.
                            if (countDelete >= 1 && countInsert >= 1)
                            {
                                // Delete the offending records and add the merged ones.
                                var diffsWithinLine = Compute(textDelete, textInsert, false, token, optimizeForSpeed);
                                var count = countDelete + countInsert;
                                var index = pointer - count;
                                diffs.Splice(index, count, diffsWithinLine);
                                pointer = index + diffsWithinLine.Count;
                            }
                            countInsert = 0;
                            countDelete = 0;
                            textDelete = string.Empty;
                            textInsert = string.Empty;
                            break;
                    }
                    pointer++;
                }
                diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.

                return diffs;
            }

            /// <summary>
            /// Find the 'middle snake' of a diff, split the problem in two
            /// and return the recursively constructed diff.
            /// See Myers 1986 paper: An O(ND) Difference Algorithm and Its Variations.
            /// </summary>
            /// <param name="text1"></param>
            /// <param name="text2"></param>
            /// <param name="token"></param>
            /// <param name="optimizeForSpeed"></param>
            /// <returns></returns>
            internal static List<Diff> MyersDiffBisect(string text1, string text2, CancellationToken token, bool optimizeForSpeed)
            {
                // Cache the text lengths to prevent multiple calls.
                var text1Length = text1.Length;
                var text2Length = text2.Length;
                var maxD = (text1Length + text2Length + 1) / 2;
                var vOffset = maxD;
                var vLength = 2 * maxD;
                var v1 = new int[vLength];
                var v2 = new int[vLength];
                for (var x = 0; x < vLength; x++)
                {
                    v1[x] = -1;
                }
                for (var x = 0; x < vLength; x++)
                {
                    v2[x] = -1;
                }
                v1[vOffset + 1] = 0;
                v2[vOffset + 1] = 0;
                var delta = text1Length - text2Length;
                // If the total number of characters is odd, then the front path will
                // collide with the reverse path.
                var front = delta % 2 != 0;
                // Offsets for start and end of k loop.
                // Prevents mapping of space beyond the grid.
                var k1Start = 0;
                var k1End = 0;
                var k2Start = 0;
                var k2End = 0;
                for (var d = 0; d < maxD; d++)
                {
                    // Bail out if cancelled.
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    // Walk the front path one step.
                    for (var k1 = -d + k1Start; k1 <= d - k1End; k1 += 2)
                    {
                        var k1Offset = vOffset + k1;
                        int x1;
                        if (k1 == -d || k1 != d && v1[k1Offset - 1] < v1[k1Offset + 1])
                        {
                            x1 = v1[k1Offset + 1];
                        }
                        else
                        {
                            x1 = v1[k1Offset - 1] + 1;
                        }
                        var y1 = x1 - k1;
                        while (x1 < text1Length && y1 < text2Length
                               && text1[x1] == text2[y1])
                        {
                            x1++;
                            y1++;
                        }
                        v1[k1Offset] = x1;
                        if (x1 > text1Length)
                        {
                            // Ran off the right of the graph.
                            k1End += 2;
                        }
                        else if (y1 > text2Length)
                        {
                            // Ran off the bottom of the graph.
                            k1Start += 2;
                        }
                        else if (front)
                        {
                            var k2Offset = vOffset + delta - k1;
                            if (k2Offset >= 0 && k2Offset < vLength && v2[k2Offset] != -1)
                            {
                                // Mirror x2 onto top-left coordinate system.
                                var x2 = text1Length - v2[k2Offset];
                                if (x1 >= x2)
                                {
                                    // Overlap detected.
                                    return BisectSplit(text1, text2, x1, y1, token, optimizeForSpeed);
                                }
                            }
                        }
                    }

                    // Walk the reverse path one step.
                    for (var k2 = -d + k2Start; k2 <= d - k2End; k2 += 2)
                    {
                        var k2Offset = vOffset + k2;
                        int x2;
                        if (k2 == -d || k2 != d && v2[k2Offset - 1] < v2[k2Offset + 1])
                        {
                            x2 = v2[k2Offset + 1];
                        }
                        else
                        {
                            x2 = v2[k2Offset - 1] + 1;
                        }
                        var y2 = x2 - k2;
                        while (x2 < text1Length && y2 < text2Length
                               && text1[text1Length - x2 - 1]
                               == text2[text2Length - y2 - 1])
                        {
                            x2++;
                            y2++;
                        }
                        v2[k2Offset] = x2;
                        if (x2 > text1Length)
                        {
                            // Ran off the left of the graph.
                            k2End += 2;
                        }
                        else if (y2 > text2Length)
                        {
                            // Ran off the top of the graph.
                            k2Start += 2;
                        }
                        else if (!front)
                        {
                            var k1Offset = vOffset + delta - k2;
                            if (k1Offset >= 0 && k1Offset < vLength && v1[k1Offset] != -1)
                            {
                                var x1 = v1[k1Offset];
                                var y1 = vOffset + x1 - k1Offset;
                                // Mirror x2 onto top-left coordinate system.
                                x2 = text1Length - v2[k2Offset];
                                if (x1 >= x2)
                                {
                                    // Overlap detected.
                                    return BisectSplit(text1, text2, x1, y1, token, optimizeForSpeed);
                                }
                            }
                        }
                    }
                }
                // Diff took too long and hit the deadline or
                // number of Diffs equals number of characters, no commonality at all.
                var diffs = new List<Diff> { Diff.Delete(text1), Diff.Insert(text2) };
                return diffs;
            }

            /// <summary>
            /// Given the location of the 'middle snake', split the diff in two parts
            /// and recurse.
            /// </summary>
            /// <param name="text1"></param>
            /// <param name="text2"></param>
            /// <param name="x">Index of split point in text1.</param>
            /// <param name="y">Index of split point in text2.</param>
            /// <param name="token"></param>
            /// <param name="optimizeForSpeed"></param>
            /// <returns></returns>
            private static List<Diff> BisectSplit(string text1, string text2, int x, int y, CancellationToken token, bool optimizeForSpeed)
            {
                var text1A = text1.Substring(0, x);
                var text2A = text2.Substring(0, y);
                var text1B = text1.Substring(x);
                var text2B = text2.Substring(y);

                // Compute both Diffs serially.
                var diffs = Compute(text1A, text2A, false, token, optimizeForSpeed);
                var diffsb = Compute(text1B, text2B, false, token, optimizeForSpeed);

                diffs.AddRange(diffsb);
                return diffs;
            }

        }

        static class TextUtil
        {

            /// <summary>
            /// Determine if the suffix of one string is the prefix of another. Returns 
            /// the number of characters common to the end of the first
            /// string and the start of the second string.
            /// </summary>
            /// <param name="text1"></param>
            /// <param name="text2"></param>
            /// <returns>The number of characters common to the end of the first
            ///  string and the start of the second string.</returns>
            internal static int CommonOverlap(string text1, string text2)
            {
                // Cache the text lengths to prevent multiple calls.
                var text1Length = text1.Length;
                var text2Length = text2.Length;
                // Eliminate the null case.
                if (text1Length == 0 || text2Length == 0)
                {
                    return 0;
                }
                // Truncate the longer string.
                if (text1Length > text2Length)
                {
                    text1 = text1.Substring(text1Length - text2Length);
                }
                else if (text1Length < text2Length)
                {
                    text2 = text2.Substring(0, text1Length);
                }
                var textLength = Math.Min(text1Length, text2Length);
                // Quick check for the worst case.
                if (text1 == text2)
                {
                    return textLength;
                }

                // Start by looking for a single character match
                // and increase length until no match is found.
                // Performance analysis: http://neil.fraser.name/news/2010/11/04/
                var best = 0;
                var length = 1;
                while (true)
                {
                    var pattern = text1.Substring(textLength - length);
                    var found = text2.IndexOf(pattern, StringComparison.Ordinal);
                    if (found == -1)
                    {
                        return best;
                    }
                    length += found;
                    if (found == 0 || text1.Substring(textLength - length) ==
                        text2.Substring(0, length))
                    {
                        best = length;
                        length++;
                    }
                }

            }
            /// <summary>
            /// Determine the common prefix of two strings as the number of characters common to the start of each string.
            /// </summary>
            /// <param name="text1"></param>
            /// <param name="text2"></param>
            /// <returns>The number of characters common to the start of each string.</returns>
            internal static int CommonPrefix(string text1, string text2)
            {
                // Performance analysis: http://neil.fraser.name/news/2007/10/09/
                var n = Math.Min(text1.Length, text2.Length);
                for (var i = 0; i < n; i++)
                {
                    if (text1[i] != text2[i])
                    {
                        return i;
                    }
                }
                return n;
            }

            /// <summary>
            /// Determine the common suffix of two strings as the number of characters common to the end of each string.
            /// </summary>
            /// <param name="text1"></param>
            /// <param name="text2"></param>
            /// <returns>The number of characters common to the end of each string.</returns>
            internal static int CommonSuffix(string text1, string text2)
            {
                // Performance analysis: http://neil.fraser.name/news/2007/10/09/
                var text1Length = text1.Length;
                var text2Length = text2.Length;
                var n = Math.Min(text1.Length, text2.Length);
                for (var i = 1; i <= n; i++)
                {
                    if (text1[text1Length - i] != text2[text2Length - i])
                    {
                        return i - 1;
                    }
                }
                return n;
            }
            /// <summary>
            /// Does a Substring of shorttext exist within longtext such that the
            /// Substring is at least half the length of longtext?
            /// </summary>
            /// <param name="longtext">Longer string.</param>
            /// <param name="shorttext">Shorter string.</param>
            /// <param name="i">Start index of quarter length Substring within longtext.</param>
            /// <returns></returns>
            private static HalfMatchResult HalfMatchI(string longtext, string shorttext, int i)
            {
                // Start with a 1/4 length Substring at position i as a seed.
                var seed = longtext.Substring(i, longtext.Length / 4);
                var j = -1;

                var bestCommon = string.Empty;
                string bestLongtextA = string.Empty, bestLongtextB = string.Empty;
                string bestShorttextA = string.Empty, bestShorttextB = string.Empty;

                while (j < shorttext.Length && (j = shorttext.IndexOf(seed, j + 1, StringComparison.Ordinal)) != -1)
                {
                    var prefixLength = CommonPrefix(longtext.Substring(i), shorttext.Substring(j));
                    var suffixLength = CommonSuffix(longtext.Substring(0, i), shorttext.Substring(0, j));
                    if (bestCommon.Length < suffixLength + prefixLength)
                    {
                        bestCommon = shorttext.Substring(j - suffixLength, suffixLength) + shorttext.Substring(j, prefixLength);
                        bestLongtextA = longtext.Substring(0, i - suffixLength);
                        bestLongtextB = longtext.Substring(i + prefixLength);
                        bestShorttextA = shorttext.Substring(0, j - suffixLength);
                        bestShorttextB = shorttext.Substring(j + prefixLength);
                    }
                }
                return bestCommon.Length * 2 >= longtext.Length
                    ? new HalfMatchResult(bestLongtextA, bestLongtextB, bestShorttextA, bestShorttextB, bestCommon)
                    : HalfMatchResult.Empty;
            }

            /// <summary>
            /// Do the two texts share a Substring which is at least half the length of
            /// the longer text?
            /// This speedup can produce non-minimal Diffs.
            /// </summary>
            /// <param name="text1"></param>
            /// <param name="text2"></param>
            /// <returns>Data structure containing the prefix and suffix of string1, 
            /// the prefix and suffix of string 2, and the common middle. Null if there was no match.</returns>
            internal static HalfMatchResult HalfMatch(string text1, string text2)
            {
                var longtext = text1.Length > text2.Length ? text1 : text2;
                var shorttext = text1.Length > text2.Length ? text2 : text1;
                if (longtext.Length < 4 || shorttext.Length * 2 < longtext.Length)
                {
                    return HalfMatchResult.Empty; // Pointless.
                }

                // First check if the second quarter is the seed for a half-match.
                var hm1 = HalfMatchI(longtext, shorttext, (longtext.Length * 3) / 4);
                // Check again based on the third quarter.
                var hm2 = HalfMatchI(longtext, shorttext, (longtext.Length * 1) / 2);

                if (hm2.IsEmpty)
                {
                    return hm1;
                }

                if (hm1.IsEmpty)
                {
                    return hm2;
                }
                // Both matched.  Select the longest.
                var hm = hm1 > hm2 ? hm1 : hm2;

                return hm;
            }
        }
        static class DiffList
        {
            /// <summary>
            /// Reduce the number of edits by eliminating semantically trivial equalities.
            /// </summary>
            /// <param name="diffs"></param>
            public static void CleanupSemantic(this List<Diff> diffs)
            {
                // Stack of indices where equalities are found.
                var equalities = new Stack<int>();
                // Always equal to equalities[equalitiesLength-1][1]
                string lastequality = null;
                var pointer = 0;  // Index of current position.
                // Number of characters that changed prior to the equality.
                var lengthInsertions1 = 0;
                var lengthDeletions1 = 0;
                // Number of characters that changed after the equality.
                var lengthInsertions2 = 0;
                var lengthDeletions2 = 0;
                while (pointer < diffs.Count)
                {
                    if (diffs[pointer].Operation == Operation.Equal)
                    {  // Equality found.
                        equalities.Push(pointer);
                        lengthInsertions1 = lengthInsertions2;
                        lengthDeletions1 = lengthDeletions2;
                        lengthInsertions2 = 0;
                        lengthDeletions2 = 0;
                        lastequality = diffs[pointer].Text;
                    }
                    else
                    {  // an insertion or deletion
                        if (diffs[pointer].Operation == Operation.Insert)
                        {
                            lengthInsertions2 += diffs[pointer].Text.Length;
                        }
                        else
                        {
                            lengthDeletions2 += diffs[pointer].Text.Length;
                        }
                        // Eliminate an equality that is smaller or equal to the edits on both
                        // sides of it.
                        if (lastequality != null && (lastequality.Length
                                                     <= Math.Max(lengthInsertions1, lengthDeletions1))
                            && (lastequality.Length
                                <= Math.Max(lengthInsertions2, lengthDeletions2)))
                        {
                            // Duplicate record.

                            diffs.Splice(equalities.Peek(), 1, Diff.Delete(lastequality), Diff.Insert(lastequality));

                            // Throw away the equality we just deleted.
                            equalities.Pop();
                            if (equalities.Count > 0)
                            {
                                equalities.Pop();
                            }
                            pointer = equalities.Count > 0 ? equalities.Peek() : -1;
                            lengthInsertions1 = 0;  // Reset the counters.
                            lengthDeletions1 = 0;
                            lengthInsertions2 = 0;
                            lengthDeletions2 = 0;
                            lastequality = null;
                        }
                    }
                    pointer++;
                }

                diffs.CleanupMerge();
                diffs.CleanupSemanticLossless();

                // Find any overlaps between deletions and insertions.
                // e.g: <del>abcxxx</del><ins>xxxdef</ins>
                //   -> <del>abc</del>xxx<ins>def</ins>
                // e.g: <del>xxxabc</del><ins>defxxx</ins>
                //   -> <ins>def</ins>xxx<del>abc</del>
                // Only extract an overlap if it is as big as the edit ahead or behind it.
                pointer = 1;
                while (pointer < diffs.Count)
                {
                    if (diffs[pointer - 1].Operation == Operation.Delete &&
                        diffs[pointer].Operation == Operation.Insert)
                    {
                        var deletion = diffs[pointer - 1].Text;
                        var insertion = diffs[pointer].Text;
                        var overlapLength1 = TextUtil.CommonOverlap(deletion, insertion);
                        var overlapLength2 = TextUtil.CommonOverlap(insertion, deletion);
                        if (overlapLength1 >= overlapLength2)
                        {
                            if (overlapLength1 >= deletion.Length / 2.0 ||
                                overlapLength1 >= insertion.Length / 2.0)
                            {
                                // Overlap found.
                                // Insert an equality and trim the surrounding edits.
                                var newDiffs = new[]
                            {
                                Diff.Delete(deletion.Substring(0, deletion.Length - overlapLength1)),
                                Diff.Equal(insertion.Substring(0, overlapLength1)),
                                Diff.Insert(insertion.Substring(overlapLength1))
                            };

                                diffs.Splice(pointer - 1, 2, newDiffs);
                                pointer++;
                            }
                        }
                        else
                        {
                            if (overlapLength2 >= deletion.Length / 2.0 ||
                                overlapLength2 >= insertion.Length / 2.0)
                            {
                                // Reverse overlap found.
                                // Insert an equality and swap and trim the surrounding edits.

                                diffs.Splice(pointer - 1, 2,
                                    Diff.Insert(insertion.Substring(0, insertion.Length - overlapLength2)),
                                    Diff.Equal(deletion.Substring(0, overlapLength2)),
                                    Diff.Delete(deletion.Substring(overlapLength2)
                                        ));
                                pointer++;
                            }
                        }
                        pointer++;
                    }
                    pointer++;
                }
            }

            /// <summary>
            /// Look for single edits surrounded on both sides by equalities
            /// which can be shifted sideways to align the edit to a word boundary.
            /// e.g: The c<ins>at c</ins>ame. -> The <ins>cat </ins>came.
            /// </summary>
            /// <param name="diffs"></param>
            public static void CleanupSemanticLossless(this List<Diff> diffs)
            {
                var pointer = 1;
                // Intentionally ignore the first and last element (don't need checking).
                while (pointer < diffs.Count - 1)
                {
                    if (diffs[pointer - 1].Operation == Operation.Equal &&
                        diffs[pointer + 1].Operation == Operation.Equal)
                    {
                        // This is a single edit surrounded by equalities.
                        var equality1 = diffs[pointer - 1].Text;
                        var edit = diffs[pointer].Text;
                        var equality2 = diffs[pointer + 1].Text;

                        // First, shift the edit as far left as possible.
                        var commonOffset = TextUtil.CommonSuffix(equality1, edit);
                        if (commonOffset > 0)
                        {
                            var commonString = edit.Substring(edit.Length - commonOffset);
                            equality1 = equality1.Substring(0, equality1.Length - commonOffset);
                            edit = commonString + edit.Substring(0, edit.Length - commonOffset);
                            equality2 = commonString + equality2;
                        }

                        // Second, step character by character right,
                        // looking for the best fit.
                        var bestEquality1 = equality1;
                        var bestEdit = edit;
                        var bestEquality2 = equality2;
                        var bestScore = DiffCleanupSemanticScore(equality1, edit) + DiffCleanupSemanticScore(edit, equality2);
                        while (edit.Length != 0 && equality2.Length != 0 && edit[0] == equality2[0])
                        {
                            equality1 += edit[0];
                            edit = edit.Substring(1) + equality2[0];
                            equality2 = equality2.Substring(1);
                            var score = DiffCleanupSemanticScore(equality1, edit) + DiffCleanupSemanticScore(edit, equality2);
                            // The >= encourages trailing rather than leading whitespace on
                            // edits.
                            if (score >= bestScore)
                            {
                                bestScore = score;
                                bestEquality1 = equality1;
                                bestEdit = edit;
                                bestEquality2 = equality2;
                            }
                        }

                        if (diffs[pointer - 1].Text != bestEquality1)
                        {
                            // We have an improvement, save it back to the diff.

                            var newDiffs = new[]
                        {
                            Diff.Equal(bestEquality1),
                            diffs[pointer].Replace(bestEdit),
                            Diff.Equal(bestEquality2)
                        }.Where(d => !string.IsNullOrEmpty(d.Text))
                                .ToArray();

                            diffs.Splice(pointer - 1, 3, newDiffs);
                            pointer = pointer - (3 - newDiffs.Length);
                        }
                    }
                    pointer++;
                }
            }

            /// <summary>
            /// Given two strings, compute a score representing whether the internal
            /// boundary falls on logical boundaries.
            /// Scores range from 6 (best) to 0 (worst).
            ///  </summary>
            /// <param name="one"></param>
            /// <param name="two"></param>
            /// <returns>score</returns>
            private static int DiffCleanupSemanticScore(string one, string two)
            {
                if (one.Length == 0 || two.Length == 0)
                {
                    // Edges are the best.
                    return 6;
                }

                // Each port of this function behaves slightly differently due to
                // subtle differences in each language's definition of things like
                // 'whitespace'.  Since this function's purpose is largely cosmetic,
                // the choice has been made to use each language's native features
                // rather than force total conformity.
                var char1 = one[one.Length - 1];
                var char2 = two[0];
                var nonAlphaNumeric1 = !Char.IsLetterOrDigit(char1);
                var nonAlphaNumeric2 = !Char.IsLetterOrDigit(char2);
                var whitespace1 = nonAlphaNumeric1 && Char.IsWhiteSpace(char1);
                var whitespace2 = nonAlphaNumeric2 && Char.IsWhiteSpace(char2);
                var lineBreak1 = whitespace1 && Char.IsControl(char1);
                var lineBreak2 = whitespace2 && Char.IsControl(char2);
                var blankLine1 = lineBreak1 && BlankLineEnd.IsMatch(one);
                var blankLine2 = lineBreak2 && BlankLineStart.IsMatch(two);

                if (blankLine1 || blankLine2)
                {
                    // Five points for blank lines.
                    return 5;
                }
                if (lineBreak1 || lineBreak2)
                {
                    // Four points for line breaks.
                    return 4;
                }
                if (nonAlphaNumeric1 && !whitespace1 && whitespace2)
                {
                    // Three points for end of sentences.
                    return 3;
                }
                if (whitespace1 || whitespace2)
                {
                    // Two points for whitespace.
                    return 2;
                }
                if (nonAlphaNumeric1 || nonAlphaNumeric2)
                {
                    // One point for non-alphanumeric.
                    return 1;
                }
                return 0;
            }

            private static readonly Regex BlankLineEnd = new Regex("\\n\\r?\\n\\Z", RegexOptions.Compiled);
            private static readonly Regex BlankLineStart = new Regex("\\A\\r?\\n\\r?\\n", RegexOptions.Compiled);

            /// <summary>
            /// Reorder and merge like edit sections.  Merge equalities.
            /// Any edit section can move as long as it doesn't cross an equality.
            /// </summary>
            /// <param name="diffs">list of Diffs</param>
            public static void CleanupMerge(this List<Diff> diffs)
            {
                // Add a dummy entry at the end.
                diffs.Add(Diff.Equal(string.Empty));
                var countDelete = 0;
                var countInsert = 0;
                var textDelete = string.Empty;
                var textInsert = string.Empty;

                var pointer = 0;
                while (pointer < diffs.Count)
                {
                    switch (diffs[pointer].Operation)
                    {
                        case Operation.Insert:
                            countInsert++;
                            textInsert += diffs[pointer].Text;
                            pointer++;
                            break;
                        case Operation.Delete:
                            countDelete++;
                            textDelete += diffs[pointer].Text;
                            pointer++;
                            break;
                        case Operation.Equal:
                            // Upon reaching an equality, check for prior redundancies.
                            if (countDelete + countInsert > 1)
                            {
                                if (countDelete != 0 && countInsert != 0)
                                {
                                    // Factor out any common prefixies.
                                    var commonlength = TextUtil.CommonPrefix(textInsert, textDelete);
                                    if (commonlength != 0)
                                    {
                                        var index = pointer - countDelete - countInsert - 1;
                                        if (index >= 0 && diffs[index].Operation == Operation.Equal)
                                        {
                                            diffs[index] =
                                                diffs[index].Replace(diffs[index].Text +
                                                                     textInsert.Substring(0, commonlength));
                                        }
                                        else
                                        {
                                            diffs.Insert(0, Diff.Equal(textInsert.Substring(0, commonlength)));
                                            pointer++;
                                        }
                                        textInsert = textInsert.Substring(commonlength);
                                        textDelete = textDelete.Substring(commonlength);
                                    }
                                    // Factor out any common suffixies.
                                    commonlength = TextUtil.CommonSuffix(textInsert, textDelete);
                                    if (commonlength != 0)
                                    {
                                        diffs[pointer] = diffs[pointer].Replace(textInsert.Substring(textInsert.Length
                                                                                                     - commonlength) +
                                                                                diffs[pointer].Text);
                                        textInsert = textInsert.Substring(0, textInsert.Length
                                                                             - commonlength);
                                        textDelete = textDelete.Substring(0, textDelete.Length
                                                                             - commonlength);
                                    }
                                }
                                // Delete the offending records and add the merged ones.
                                List<Diff> newDiffs = new List<Diff>();
                                if (countDelete > 0 && !string.IsNullOrEmpty(textDelete))
                                {
                                    newDiffs.Add(Diff.Delete(textDelete));
                                }
                                if (countInsert > 0 && !string.IsNullOrEmpty(textInsert))
                                {
                                    newDiffs.Add(Diff.Insert(textInsert));
                                }

                                diffs.Splice(pointer - countDelete - countInsert, countDelete + countInsert, newDiffs);
                                pointer = pointer - countDelete - countInsert + newDiffs.Count + 1;
                            }
                            else if (pointer != 0
                                     && diffs[pointer - 1].Operation == Operation.Equal)
                            {
                                // Merge this equality with the previous one.
                                diffs[pointer - 1] = diffs[pointer - 1].Replace(diffs[pointer - 1].Text + diffs[pointer].Text);
                                diffs.RemoveAt(pointer);
                            }
                            else
                            {
                                pointer++;
                            }
                            countInsert = 0;
                            countDelete = 0;
                            textDelete = string.Empty;
                            textInsert = string.Empty;
                            break;
                    }
                }
                if (diffs[diffs.Count - 1].Text.Length == 0)
                {
                    diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.
                }

                // Second pass: look for single edits surrounded on both sides by
                // equalities which can be shifted sideways to eliminate an equality.
                // e.g: A<ins>BA</ins>C -> <ins>AB</ins>AC
                var changes = false;
                pointer = 1;
                // Intentionally ignore the first and last element (don't need checking).
                while (pointer < diffs.Count - 1)
                {
                    if (diffs[pointer - 1].Operation == Operation.Equal &&
                        diffs[pointer + 1].Operation == Operation.Equal)
                    {
                        // This is a single edit surrounded by equalities.
                        if (diffs[pointer].Text.EndsWith(diffs[pointer - 1].Text,
                            StringComparison.Ordinal))
                        {
                            // Shift the edit over the previous equality.
                            var text = diffs[pointer - 1].Text +
                                       diffs[pointer].Text.Substring(0, diffs[pointer].Text.Length -
                                                                        diffs[pointer - 1].Text.Length);
                            diffs[pointer] = diffs[pointer].Replace(text);
                            diffs[pointer + 1] = diffs[pointer + 1].Replace(diffs[pointer - 1].Text
                                                                            + diffs[pointer + 1].Text);
                            diffs.Splice(pointer - 1, 1);
                            changes = true;
                        }
                        else if (diffs[pointer].Text.StartsWith(diffs[pointer + 1].Text,
                            StringComparison.Ordinal))
                        {
                            // Shift the edit over the next equality.
                            diffs[pointer - 1] = diffs[pointer - 1].Replace(diffs[pointer - 1].Text + diffs[pointer + 1].Text);
                            diffs[pointer] = diffs[pointer].Replace(diffs[pointer].Text.Substring(diffs[pointer + 1].Text.Length)
                                                                    + diffs[pointer + 1].Text);
                            diffs.Splice(pointer + 1, 1);
                            changes = true;
                        }
                    }
                    pointer++;
                }
                // If shifts were made, the diff needs reordering and another shift sweep.
                if (changes)
                {
                    diffs.CleanupMerge();
                }
            }
            /// <summary>
            /// replaces [count] entries starting at index [start] with the given [objects]
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="input"></param>
            /// <param name="start"></param>
            /// <param name="count"></param>
            /// <param name="objects"></param>
            /// <returns>the deleted objects</returns>
            internal static List<T> Splice<T>(this List<T> input, int start, int count, IEnumerable<T> objects)
            {
                var deletedRange = input.GetRange(start, count);
                input.RemoveRange(start, count);
                input.InsertRange(start, objects);
                return deletedRange;
            }
            internal static List<T> Splice<T>(this List<T> input, int start, int count, params T[] objects)
            {
                IEnumerable<T> enumerable = objects;
                return input.Splice(start, count, enumerable);
            }
            internal static IEnumerable<string> SplitLines(this string text)
            {
                var lineStart = 0;
                var lineEnd = -1;
                while (lineEnd < text.Length - 1)
                {

                    lineEnd = text.IndexOf('\n', lineStart);
                    if (lineEnd == -1)
                    {
                        lineEnd = text.Length - 1;
                    }
                    var line = text.Substring(lineStart, lineEnd + 1 - lineStart);
                    yield return line;
                    lineStart = lineEnd + 1;
                }
            }
        }
        struct HalfMatchResult : IEquatable<HalfMatchResult>
        {
            public HalfMatchResult(string prefix1, string suffix1, string prefix2, string suffix2, string commonMiddle)
            {
                Prefix1 = prefix1 ?? throw new ArgumentNullException(nameof(prefix1));
                Suffix1 = suffix1 ?? throw new ArgumentNullException(nameof(suffix1));
                Prefix2 = prefix2 ?? throw new ArgumentNullException(nameof(prefix2));
                Suffix2 = suffix2 ?? throw new ArgumentNullException(nameof(suffix2));
                CommonMiddle = commonMiddle ?? throw new ArgumentNullException(nameof(commonMiddle));
            }

            public readonly string Prefix1;
            public readonly string Suffix1;
            public readonly string CommonMiddle;
            public readonly string Prefix2;
            public readonly string Suffix2;
            public bool IsEmpty => string.IsNullOrEmpty(CommonMiddle);

            public static readonly HalfMatchResult Empty = new HalfMatchResult();

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (obj.GetType() != GetType()) return false;
                return Equals((HalfMatchResult)obj);
            }

            public bool Equals(HalfMatchResult other)
            {
                return string.Equals(Prefix1, other.Prefix1) 
                    && string.Equals(Suffix1, other.Suffix1) 
                    && string.Equals(CommonMiddle, other.CommonMiddle) 
                    && string.Equals(Prefix2, other.Prefix2) 
                    && string.Equals(Suffix2, other.Suffix2);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Prefix1 != null ? Prefix1.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Suffix1 != null ? Suffix1.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (CommonMiddle != null ? CommonMiddle.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Prefix2 != null ? Prefix2.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Suffix2 != null ? Suffix2.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(HalfMatchResult left, HalfMatchResult right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(HalfMatchResult left, HalfMatchResult right)
            {
                return !Equals(left, right);
            }

            public static bool operator >(HalfMatchResult left, HalfMatchResult right)
            {
                return left.CommonMiddle.Length > right.CommonMiddle.Length;
            }

            public static bool operator <(HalfMatchResult left, HalfMatchResult right)
            {
                return left.CommonMiddle.Length < right.CommonMiddle.Length;
            }
        }
        class LineToCharCompressor
        {
            // e.g. _lineArray[4] == "Hello\n"
            // e.g. _lineHash["Hello\n"] == 4
            readonly List<string> _lineArray = new List<string>();
            readonly Dictionary<string, int> _lineHash = new Dictionary<string, int>();

            /// <summary>
            /// Compresses all lines of a text to a series of indexes (starting at \u0001, ending at (char)text.Length)
            /// </summary>
            /// <param name="text"></param>
            /// <returns></returns>
            public string Compress(string text)
            {
                var sb = new StringBuilder();
                foreach (var line in text.SplitLines())
                {
                    if (!_lineHash.ContainsKey(line))
                    {
                        _lineArray.Add(line);
                        // "\u0000" is a valid character, but various debuggers don't like it. 
                        // Therefore, add Count, not Count - 1
                        _lineHash.Add(line, _lineArray.Count);
                    }
                    sb.Append((char)_lineHash[line]);
                }
                return sb.ToString();
            }

            public string Decompress(string text)
            {
                var sb = new StringBuilder();
                foreach (var c in text)
                {
                    sb.Append(_lineArray[c - 1]);
                }
                return sb.ToString();
            }
          
        }
    }
#endregion  

}
