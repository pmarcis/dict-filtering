using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CreateMultilingualTranslitDictionary
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> inDirList = new List<string>();
            string outFile = null;
            string extension = "translit";
            
            for (int i = 0; i < args.Length; i++)
            {

                if (args[i] == "-i" && args.Length > i + 2)
                {
                    int count = Convert.ToInt32(args[i + 1]);
                    for (int j = 2; j < count+2;j++ )
                    {
                        if (args.Length>i+j)
                        {
                            inDirList.Add(args[i + j]);
                        }
                    }
                }
                else if (args[i] == "-e" && args.Length > i + 1)
                {
                    extension = args[i + 1];
                }
                else if (args[i] == "-o" && args.Length > i + 1)
                {
                    outFile = args[i + 1];
                }
            }

            char[] sep = { '\t' };

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.CurrencyDecimalSeparator = ".";
            nfi.NumberDecimalSeparator = ".";
            nfi.PercentDecimalSeparator = ".";

            string srcLang = "en";
            Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<int,double>>>> dataDict = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<int,double>>>>();
            Dictionary<string, bool> langDict = new Dictionary<string, bool>();

            int iterationCounter = 0;

            foreach (string inDir in inDirList)
            {
                iterationCounter++;
                foreach (string file in Directory.GetFiles(inDir, "*." + extension, SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file);
                    if (name.StartsWith(srcLang))
                    {
                        string trgLang = name.Substring(3, 2);
                        if (!langDict.ContainsKey(trgLang)) langDict.Add(trgLang, true);
                        StreamReader sr = new StreamReader(file, Encoding.UTF8);
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine().Trim();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                string[] arr = line.Split(sep, StringSplitOptions.None);
                                if (arr.Length == 3)
                                {
                                    double prob = Convert.ToDouble(arr[2], nfi);
                                    if (!dataDict.ContainsKey(arr[0])) dataDict.Add(arr[0], new Dictionary<string, Dictionary<string, Dictionary<int, double>>>());
                                    if (!dataDict[arr[0]].ContainsKey(trgLang)) dataDict[arr[0]].Add(trgLang, new Dictionary<string, Dictionary<int, double>>());
                                    if (!dataDict[arr[0]][trgLang].ContainsKey(arr[1])) dataDict[arr[0]][trgLang].Add(arr[1], new Dictionary<int, double>());
                                    if (!dataDict[arr[0]][trgLang][arr[1]].ContainsKey(iterationCounter)) dataDict[arr[0]][trgLang][arr[1]].Add(iterationCounter, prob);
                                }
                                else if (arr.Length == 2)
                                {
                                    double prob = 1.0;
                                    string src = arr[0];
                                    string trg = arr[1];
                                    if (iterationCounter==1)
                                    {
                                        //As the initial data did not contain (that is, I forgot to add) the probabilities, we need to calculate them!
                                        src = FilterGizaDictionary.SimpleCharacterTransliteration.Transliterate(src);
                                        trg = FilterGizaDictionary.SimpleCharacterTransliteration.Transliterate(trg);
                                        double levenshtainDistance = FilterGizaDictionary.LevenshteinDistance.Compute (src, trg);
							            double maxLen = Math.Max (src.Length, trg.Length);
							            prob = (maxLen - levenshtainDistance) / maxLen;
                                    }
                                    if (!dataDict.ContainsKey(arr[0])) dataDict.Add(arr[0], new Dictionary<string, Dictionary<string, Dictionary<int, double>>>());
                                    if (!dataDict[arr[0]].ContainsKey(trgLang)) dataDict[arr[0]].Add(trgLang, new Dictionary<string, Dictionary<int, double>>());
                                    if (!dataDict[arr[0]][trgLang].ContainsKey(arr[1])) dataDict[arr[0]][trgLang].Add(arr[1], new Dictionary<int, double>());
                                    if (!dataDict[arr[0]][trgLang][arr[1]].ContainsKey(iterationCounter)) dataDict[arr[0]][trgLang][arr[1]].Add(iterationCounter, prob);
                                }
                            }
                        }
                        sr.Close();
                    }
                }
            }

            TranslitCollection tc = new TranslitCollection();

            List<string> srcWords = dataDict.Keys.ToList();
            srcWords.Sort();

            List<string> langs = langDict.Keys.ToList();
            langs.Sort();

            Dictionary<string, double> translCount = new Dictionary<string, double>();
            Dictionary<string, double> wordCount = new Dictionary<string, double>();
            foreach (string lang in langs)
            {
                translCount.Add(lang, 0);
                wordCount.Add(lang, 0);
            }

            int totalWords = 0;
            int totalTranslPairs = 0;

            foreach (string srcWord in srcWords)
            {
                SEntry c = new SEntry();
                c.str = srcWord;
                c.tEntries = new List<TEntry>();

                if (dataDict[srcWord].Count == langs.Count)
                {
                    Console.WriteLine(srcWord);
                }

                totalWords++;

                foreach (string lang in langs)
                {
                    if (dataDict[srcWord].ContainsKey(lang))
                    {
                        wordCount[lang]++;
                        //LEntry l = new LEntry();
                        //l.tEntries = new List<TEntry>();
                        List<KeyValuePair<string, Dictionary<int, double>>> transls = dataDict[srcWord][lang].ToList();
                        //l.lang = lang;
                        transls.Sort(Compare);
                        foreach (KeyValuePair<string, Dictionary<int, double>> kvp in transls)
                        {
                            int min = int.MaxValue;
                            foreach (int key in kvp.Value.Keys)
                            {
                                if (key < min) min = key;
                            }
                            totalTranslPairs++;
                            translCount[lang]++;
                            TEntry t = new TEntry();
                            t.lang = lang;
                            t.str = kvp.Key;
                            t.score = kvp.Value[min];
                            t.iteration = min;
                            c.tEntries.Add(t);
                        }
                        //c.tEntries.Add(l);
                    }
                }
                tc.cEntries.Add(c);
            }

            string outputStr = SerializeObjectInstance<TranslitCollection>(tc);
            File.WriteAllText(outFile, outputStr, new UTF8Encoding(false));


            Console.WriteLine("Total number of collection entries: " + totalWords.ToString());
            Console.WriteLine("Total number of transliteration pairs: " + totalTranslPairs.ToString());

            Console.WriteLine("Language\tWords\tTransliterations\tAVG per word");
            foreach (string lang in langs)
            {
                Console.Write(lang);
                Console.Write("\t");
                Console.Write(wordCount[lang]);
                Console.Write("\t");
                Console.Write(translCount[lang]);
                Console.Write("\t");
                Console.WriteLine(translCount[lang] / wordCount[lang]);
            }
        }

        static int Compare(KeyValuePair<string, Dictionary<int, double>> a, KeyValuePair<string, Dictionary<int, double>> b)
        {
            int minA = int.MaxValue;
            int minB = int.MaxValue;
            foreach (int key in a.Value.Keys)
            {
                if (key < minA) minA = key;
            }
            foreach (int key in b.Value.Keys)
            {
                if (key < minB) minB = key;
            }
            if (minA!=minB)
            {
                return minA.CompareTo(minB);
            }
            else
            {
                return b.Value[minB].CompareTo(a.Value[minA]);
            }
        }

        
        

        /// <summary>
        /// Generic deserialization function. Deserializes a string to a target type object.
        /// </summary>
        /// <typeparam name="T">The target type, to which a given string will be deserialized.</typeparam>
        /// <param name="stringToDeserialize">The string that will be deserialized.</param>
        /// <returns>An instance of the target type, which is the deserialized string.</returns>
        public static T DeserializeString<T>(string stringToDeserialize)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            //System.IO.MemoryStream mem = new System.IO.MemoryStream();
            StringReader sr = new StringReader(stringToDeserialize);
            T deserializationResult = (T)ser.Deserialize(sr);
            sr.Close();
            return deserializationResult;
        }

        /// <summary>
        /// Serializes the object instance.
        /// </summary>
        /// <typeparam name="T">The source type, which instance will be serialized.</typeparam>
        /// <param name="objectInstance">The object instance.</param>
        /// <returns>A serialized string of the given object instance.</returns>
        public static string SerializeObjectInstance<T>(T objectInstance)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            //System.IO.MemoryStream mem = new System.IO.MemoryStream();
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            ser.Serialize(sw, objectInstance);
            sw.Close();
            return sb.ToString();
        }
    }

    [Serializable]
    public class TranslitCollection
    {
        public TranslitCollection()
        {
            cEntries = new List<SEntry>();
        }
        [XmlElement("SEntry")]
        public List<SEntry> cEntries = new List<SEntry>();
    }

    [Serializable]
    public class SEntry
    {
        public SEntry()
        {
            str = null;
            tEntries = null;
        }

        [XmlAttribute("str")]
        public string str = null;
        [XmlElement("TEntry")]
        public List<TEntry> tEntries = null;
    }

    [Serializable]
    public class LEntry
    {
        public LEntry()
        {
            lang = null;
            tEntries = null;
        }

        [XmlAttribute("lang")]
        public string lang = null;
        [XmlElement("TEntry")]
        public List<TEntry> tEntries = null;
    }

    [Serializable]
    public class TEntry
    {
        [XmlIgnore]
        NumberFormatInfo nfi = new NumberFormatInfo();

        public TEntry()
        {
            nfi = new NumberFormatInfo();
            score = null;
            str = null;
            nfi.CurrencyDecimalSeparator = ".";
            nfi.NumberDecimalSeparator = ".";
            nfi.PercentDecimalSeparator = ".";
        }

        [XmlAttribute("lang")]
        public string lang = null;

        [XmlAttribute("str")]
        public string str;
        [XmlIgnore]
        public double? score;

        [XmlAttribute("iteration")]
        public int iteration=-1;

        [XmlAttribute("score")]
        public string Score
        {
            get
            {
                if (score.HasValue) return Math.Round(score.Value,4).ToString(nfi);
                return null;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value)) score = Convert.ToDouble(value, nfi);
                else score = null;
            }
        }
    }
}
