using Bogus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static System.Collections.Specialized.BitVector32;

namespace Polidiastates
{
    class Program
    {
        static void Main(string[] args)
        {
            // Creates a list of user data model for the employees
            var documents = new Faker<TextDocument>()
                .RuleFor(x => x.Text, faker => faker.Lorem.Text())
                .Generate(10000);

            // Create the indexing
            var indexed = PerformIndexing(documents, documents.SelectMany(x => x.Words).Distinct());



            Console.ReadLine();
        }

        private static Dictionary<int, HashSet<TextDocument>> PerformIndexing(IEnumerable<TextDocument> documents, IEnumerable<string> indexes) 
            => indexes.Select(x => x.ToLower()).ToDictionary(x => x.GetHashCode(), x => documents.Where(y => y.Words.Contains(x)).ToHashSet());
    }

    /// <summary>
    /// Represents a text document
    /// </summary>
    public class TextDocument
    {
        #region Private Members

        /// <summary>
        /// The member of the <see cref="Text"/>
        /// </summary>
        private string mText;

        /// <summary>
        /// The member of the <see cref="Hash"/> property
        /// </summary>
        private int? mHash;

        #endregion

        #region Public Properties

        /// <summary>
        /// The text
        /// </summary>
        public string Text
        {
            get => mText;

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Words = null;

                    return;
                }

                mText = value;

                Words = value.StripPunctuation().ToLower().Split(' ');
            }
        }

        /// <summary>
        /// The word of the text
        /// </summary>
        public string[] Words { get; private set; }

        /// <summary>
        /// The hash
        /// </summary>
        public int Hash
        {
            get
            {
                if (mHash == null)
                    mHash = HashCode.Combine(Text);

                return mHash.Value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public TextDocument() : base()
        {

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a string that represents the current object
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Text;

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        /// <returns></returns>
        public override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        /// Serves as the default hash function
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Hash;

        #endregion
    }

    /// <summary>
    /// Extension methods for <see cref="string"/>s
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Remove the punctuation from the specified <paramref name="s"/>
        /// </summary>
        /// <param name="s">The string</param>
        /// <returns></returns>
        public static string StripPunctuation(this string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s)
            {
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }

    public class LSH
    {
        #region Public Properties

        /// <summary>
        /// The indexes mapper
        /// </summary>
        public Dictionary<int, HashSet<TextDocument>> Indexes { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="indexes">The indexes</param>
        public LSH(Dictionary<int, HashSet<TextDocument>> indexes) : base()
        {
            Indexes = indexes ?? throw new ArgumentNullException(nameof(indexes));


        }

        #endregion
    }

    public class LSH2
    {
        Dictionary<int, HashSet<int>> m_lshBuckets = new Dictionary<int, HashSet<int>>();

        int[,] minHashes;
        int numBands;
        int numHashFunctions;
        int rowsPerBand;
        int numSets;
        List<SortedList<int, List<int>>> lshBuckets = new List<SortedList<int, List<int>>>();

        public LSH2(int[,] minHashes, int rowsPerBand)
        {
            this.minHashes = minHashes;
            numHashFunctions = minHashes.GetUpperBound(1) + 1;
            numSets = minHashes.GetUpperBound(0) + 1;
            this.rowsPerBand = rowsPerBand;
            this.numBands = numHashFunctions / rowsPerBand;
        }

        public void Calc()
        {
            var thisHash = 0;

            for (var b = 0; b < numBands; b++)
            {
                var thisSL = new SortedList<int, List<int>>();
                for (var s = 0; s < numSets; s++)
                {
                    var hashValue = 0;
                    for (var th = thisHash; th < thisHash + rowsPerBand; th++)
                    {
                        hashValue = unchecked(hashValue * 1174247 + minHashes[s, th]);
                    }
                    if (!thisSL.ContainsKey(hashValue))
                    {
                        thisSL.Add(hashValue, new List<int>());
                    }
                    thisSL[hashValue].Add(s);
                }
                thisHash += rowsPerBand;
                var copy = new SortedList<int, List<int>>();
                foreach (var ic in thisSL.Keys)
                {
                    if (thisSL[ic].Count() > 1)
                    {
                        copy.Add(ic, thisSL[ic]);
                    }
                }
                lshBuckets.Add(copy);
            }
        }

        public List<int> GetNearest(int n)
        {
            var nearest = new List<int>();
            foreach (var b in lshBuckets)
            {
                foreach (var li in b.Values)
                {
                    if (li.Contains(n))
                    {
                        nearest.AddRange(li);
                        break;
                    }
                }
            }
            nearest = nearest.Distinct().ToList();
            nearest.Remove(n);  // remove the document itself
            return nearest;
        }
    }
}
