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
            // Creates a list of documents
            var documents = new Faker<TextDocument>()
                .RuleFor(x => x.Text, faker => faker.Lorem.Sentence())
                .Generate(10000);



            var lsh = new LSH(documents, 50000);

            var lshSearch = lsh.Search("cum");
            var linearSearch = documents.Where(x => x.Text.Split(" ").Contains("cum")).ToList();

            Console.ReadLine();
        }

        
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

    /// <summary>
    /// The band
    /// </summary>
    public struct Band
    {
        #region Public Properties

        /// <summary>
        /// The min hash value
        /// </summary>
        public int MinHash { get; }

        /// <summary>
        /// The max hash value
        /// </summary>
        public int MaxHash { get; }

        /// <summary>
        /// The buckets
        /// </summary>
        public Dictionary<int, HashSet<TextDocument>> Buckets { get; }

        /// <summary>
        /// The number of buckets
        /// </summary>
        public int NumberOfBuckets { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="minHash">The min hash</param>
        /// <param name="maxHash">The max hash</param>
        /// <param name="buckets">The buckets</param>
        public Band(int minHash, int maxHash, Dictionary<int, HashSet<TextDocument>> buckets)
        {
            MinHash = minHash;
            MaxHash = maxHash;
            Buckets = buckets;
            NumberOfBuckets = buckets.Count();
        }

        #endregion
    }

    /// <summary>
    /// The LSH
    /// </summary>
    public class LSH
    {
        #region Public Properties

        /// <summary>
        /// The hash offset used for creating the bands
        /// </summary>
        public int HashOffset { get; }

        /// <summary>
        /// The bads
        /// </summary>
        public IEnumerable<Band> Bands { get; }

        /// <summary>
        /// The indexes mapper
        /// </summary>
        public SortedDictionary<int, HashSet<TextDocument>> Indexes { get; }

        /// <summary>
        /// The documents
        /// </summary>
        public IEnumerable<TextDocument> Documents { get; }

        /// <summary>
        /// The words
        /// </summary>
        public IEnumerable<string> Words { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="documents">The documents</param>
        /// <param name="hashOffset">The hash offset used for creating the bands</param>
        public LSH(IEnumerable<TextDocument> documents, int hashOffset) : base()
        {
            Documents = documents ?? throw new ArgumentNullException(nameof(documents));
            HashOffset = hashOffset;
            Words = documents.SelectMany(x => x.Words).Distinct().ToList();

            Indexes = PerformIndexing(documents);

            var bands = new List<Band>();

            for(var i = 0; i<= Indexes.Count - 1; i++)
            {
                var hash = Indexes.Keys.ElementAt(i);

                var maxHash = hash + (2 * HashOffset);

                // Create the band
                var band = new Band(hash, maxHash, Indexes.Where(x => x.Key >= hash && x.Key <= maxHash).ToDictionary(x => x.Key, x => x.Value));

                bands.Add(band);

                i += band.NumberOfBuckets - 1;
            }

            Bands = bands;
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// Searches using the specified <paramref name="word"/>
        /// </summary>
        /// <param name="word">The word</param>
        /// <returns></returns>
        public IEnumerable<TextDocument> Search(string word)
        {
            // Get the hash of the word
            var hash = word.GetHashCode();

            // Get the documents from the bands
            return Bands.Where(x => x.MinHash <= hash && x.MaxHash >= hash).SelectMany(x => x.Buckets.Values.SelectMany(y => y)).ToList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Performs the indexing using the specified <paramref name="documents"/>
        /// </summary>
        /// <param name="documents">The documents</param>
        /// <returns></returns>
        private SortedDictionary<int, HashSet<TextDocument>> PerformIndexing(IEnumerable<TextDocument> documents)
            => new SortedDictionary<int, HashSet<TextDocument>>(documents.SelectMany(x => x.Words).Distinct().Select(x => x.ToLower()).ToDictionary(x => x.GetHashCode(), x => documents.Where(y => y.Words.Contains(x)).ToHashSet()));

        #endregion
    }
}
