//
//  Program.cs
//
//  Author:
//       Mārcis Pinnis <marcis.pinnis@gmail.com>
//
//  Copyright (c) 2014 Mārcis Pinnis
//
//  This program can be freely used only for scientific and educational purposes.
//
//  This program is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System;
using System.Xml.Serialization;

namespace FilterGizaDictionary
{
	class MainClass
	{
		static NumberFormatInfo nfi;
		public static Dictionary<string, Dictionary<string, string>> stemDictionary = new Dictionary<string, Dictionary<string, string>>();

		/// <summary>
		/// Generic deserialization function. Deserializes a string to a target type object.
		/// </summary>
		/// <typeparam name="T">The target type, to which a given string will be deserialized.</typeparam>
		/// <param name="stringToDeserialize">The string that will be deserialized.</param>
		/// <returns>An instance of the target type, which is the deserialized string.</returns>
		public static T DeserializeString<T>( string stringToDeserialize )
		{
			XmlSerializer ser = new XmlSerializer( typeof( T ) );
			//System.IO.MemoryStream mem = new System.IO.MemoryStream();
			StringReader sr = new StringReader( stringToDeserialize );
			T deserializationResult = ( T ) ser.Deserialize( sr );
			sr.Close();
			return deserializationResult;
		}

		/// <summary>
		/// Serializes the object instance.
		/// </summary>
		/// <typeparam name="T">The source type, which instance will be serialized.</typeparam>
		/// <param name="objectInstance">The object instance.</param>
		/// <returns>A serialized string of the given object instance.</returns>
		public static string SerializeObjectInstance<T>( T objectInstance )
		{
			XmlSerializer ser = new XmlSerializer( typeof( T ) );
			//System.IO.MemoryStream mem = new System.IO.MemoryStream();
			StringBuilder sb = new StringBuilder();
			StringWriter sw = new StringWriter(sb);
			ser.Serialize( sw , objectInstance );
			sw.Close();
			return sb.ToString();
		}

		static void SaveBackupTranslits (string srcTranslitBackup, Dictionary<string, List<StringProbabEntry>> translitSrcDict)
		{
            List<KeyValueEntry> toSave = new List<KeyValueEntry>();
			foreach (string term in translitSrcDict.Keys) {
                KeyValueEntry kvp = new KeyValueEntry(term, new List<StringProbabEntry>());
				kvp.valueList.AddRange (translitSrcDict [term]);
				toSave.Add (kvp);
			}
            string outputStr = SerializeObjectInstance < List<KeyValueEntry>>(toSave);
			File.WriteAllText(srcTranslitBackup,outputStr,Encoding.UTF8);
		}

		static Dictionary<string, List<StringProbabEntry>> LoadBackupTranslits (string srcTranslitBackup)
		{
            /*Dictionary<string, List<StringProbabEntry>> tmp = new Dictionary<string, List<StringProbabEntry>>();
            tmp.Add("ABC", new List<StringProbabEntry>());
            tmp["ABC"].Add(new StringProbabEntry("strABC", 1.56));
            tmp.Add("BSD", new List<StringProbabEntry>());
            SaveBackupTranslits(srcTranslitBackup + ".tmp", tmp);
            */
			Dictionary<string, List<StringProbabEntry>> res = new Dictionary<string, List<StringProbabEntry>> ();
			string inputStr = File.ReadAllText(srcTranslitBackup,Encoding.UTF8);
            List<KeyValueEntry> fromBackup = DeserializeString<List<KeyValueEntry>>(inputStr);
            foreach (KeyValueEntry kvp in fromBackup)
            {
				if (!res.ContainsKey (kvp.Key)) {
					res.Add (kvp.Key, new List<StringProbabEntry>());
					res [kvp.Key].AddRange (kvp.valueList);
				}
			}
			return res;
		}

		static void AppendToErr (string srcTextWCap, string trgTextWCap, double prob, StreamWriter swe, string filterType)
		{
			swe.Write (srcTextWCap);
			swe.Write (" ");
			swe.Write (trgTextWCap);
			swe.Write (" ");
            swe.Write(prob.ToString(nfi));
			swe.Write (" ");
			swe.WriteLine (filterType);
		}

		public static void Main (string[] args)
		{
			nfi = new NumberFormatInfo();
			nfi.CurrencyDecimalSeparator = ".";
			nfi.NumberDecimalSeparator = ".";
			nfi.PercentDecimalSeparator = ".";
			string gizaDictionaryFile = null;
			string srcIdfFile = null;
			string trgIdfFile = null;
			string filteredDictionaryFile = null;
			Dictionary<string, double> srcIdfDict = null;
			Dictionary<string, double> trgIdfDict = null;
			string sourceLang = null;
			string targetLang = null;
			string mosesPath = "/home/marcis/LU/moses/mosesdecoder/bin/moses";
			string tempFilePath = null;
			bool useHeavyTranslit = false;

			double idfPropThreshold = 0.1;
			double absThreshold = 0.01;
			double initialThreshold = 0.1;
			double afterTranslThreshold = 0.4;
			//bool keepCapitalisation = false;
			for (int i = 0; i < args.Length; i++)
			{
				if (args [i] == "-o" && args.Length > i + 1) {
					filteredDictionaryFile = args [i + 1];
				} else if (args [i] == "-sl" && args.Length > i + 1) {
					sourceLang = args [i + 1];
				} else if (args [i] == "-tl" && args.Length > i + 1) {
					targetLang = args [i + 1];
				} else if (args [i] == "-i" && args.Length > i + 1) {
					gizaDictionaryFile = args [i + 1];
				} else if (args [i] == "-s_idf" && args.Length > i + 1) {
					srcIdfFile = args [i + 1];
				} else if (args [i] == "-t_idf" && args.Length > i + 1) {
					trgIdfFile = args [i + 1];
				} else if (args [i] == "-t") {
					useHeavyTranslit = true;
				}
			}

			if (string.IsNullOrWhiteSpace(filteredDictionaryFile)
				||string.IsNullOrWhiteSpace(gizaDictionaryFile)
				||string.IsNullOrWhiteSpace(sourceLang)
				||string.IsNullOrWhiteSpace(targetLang)
				||string.IsNullOrWhiteSpace(srcIdfFile)
				||string.IsNullOrWhiteSpace(trgIdfFile)) {
                    Console.WriteLine("Usage: ./FilterGizaDictionary.exe -i ./gizadict-in/lex.e2f -sl bg -tl en -o ./bg-en-filtered-dict.txt -s_idf ./bg_idf.txt -t_idf ./en_idf.txt -t 0.1 [-kc]");
                    Console.WriteLine("Note that Giza++ gives probabilities in a reverse order - for each target (source <tab> target <tab> probability) word the probabilities sum to one. Therefore, to get a source-to-target filtered dictionary, you have to pass the target-to-source dictionary to the input data.");
				return;
			}

			tempFilePath = filteredDictionaryFile + ".tmp";
            string dir = Path.GetDirectoryName (tempFilePath);
            if (!dir.EndsWith (Path.DirectorySeparatorChar.ToString ()))
                dir += Path.DirectorySeparatorChar.ToString ();
            string srcTranslitBackup = dir + sourceLang + "-" + targetLang + ".transliterated_words.txt";
            string trgTranslitBackup = dir + targetLang + "-" + sourceLang + ".transliterated_words.txt";

			Stemmer srcStemmer = GetStemmer (sourceLang);
			Stemmer trgStemmer = GetStemmer (targetLang);


			//Simple style for transliteration purposes starts here
			Encoding utf8WithoutBom = new UTF8Encoding(false);
            StreamWriter sw = new StreamWriter(filteredDictionaryFile, false, utf8WithoutBom);
            StreamWriter swt = new StreamWriter(filteredDictionaryFile + ".translit", false, utf8WithoutBom);
            StreamWriter swte = new StreamWriter(filteredDictionaryFile + ".translit.e", false, utf8WithoutBom);
			StreamWriter swe = new StreamWriter(filteredDictionaryFile+".e", false, utf8WithoutBom);
			sw.NewLine = "\n";
			swe.NewLine = "\n";
			if (!stemDictionary.ContainsKey(sourceLang))
			{
				stemDictionary.Add(sourceLang, new Dictionary<string, string>());
			}
			if (!stemDictionary.ContainsKey(targetLang))
			{
				stemDictionary.Add(targetLang, new Dictionary<string, string>());
			}


			Console.Write ("Reading IDF files...");

			srcIdfDict = ParseIdfFile (srcIdfFile, Encoding.UTF8, srcStemmer, sourceLang);
			double maxSrcIdf = GetMaxIdf (srcIdfDict);
			trgIdfDict = ParseIdfFile (trgIdfFile, Encoding.UTF8, trgStemmer, targetLang);
			double maxTrgIdf = GetMaxIdf (trgIdfDict);

			Console.WriteLine (" done!");


			StreamReader sr = new StreamReader(gizaDictionaryFile, Encoding.UTF8);
			Console.WriteLine("Reading dictionary file...");
			char[] sep = { ' ', '\t' };
			int counter = 0;

			//Dictionary<string, Dictionary<string, bool>> srcStems = new Dictionary<string, Dictionary<string, bool>>();
			//Dictionary<string, Dictionary<string, bool>> trgStems = new Dictionary<string, Dictionary<string, bool>>();
			Dictionary<string, Dictionary<string, double>> srcToTrgStems = new Dictionary<string, Dictionary<string, double>>();

			Dictionary<string,bool> srcWordsForTranslit = new Dictionary<string, bool> ();
			Dictionary<string,bool> trgWordsForTranslit = new Dictionary<string, bool> ();
			Dictionary<string,Dictionary<string,double>> dict = new Dictionary<string, Dictionary<string, double>> ();

			double totalCounter = 0;
			double invalidStringCounter = 0;

			//At first, we read the dictionary into memory and calculate the first scores. Note that this requires enormous memory (more than 1GB).
			//Therefore, dictionaries should not be extremely large. This has been done just for convenience and could potentially be also changed.
			while (!sr.EndOfStream) {
				counter++;
				if (counter % 10000 == 0) {
					Console.Error.Write (".");
					if (counter % 500000 == 0) {
						Console.Error.WriteLine (" - " + counter.ToString ());
					}
				}
				string line = sr.ReadLine ().Trim ();
				if (!string.IsNullOrWhiteSpace (line)) {
					string[] arr = line.Split (sep, StringSplitOptions.RemoveEmptyEntries);
					if (arr != null && arr.Length == 3) {
						totalCounter++;
						string srcText = "";
						string trgText = "";
						string srcTextWCap = arr [1].Trim();//GIZA++ probabilities are in the reverse direction! For each target word the probabilities sum to 1!
						string trgTextWCap = arr [0].Trim();
						srcText = arr [1].ToLower ().Trim ();
						trgText = arr [0].ToLower ().Trim ();
						if (string.IsNullOrWhiteSpace (srcText) || string.IsNullOrWhiteSpace (trgText)) {
							invalidStringCounter++;
							continue;
						}
						double prob = 0;
						try
						{
							prob = Convert.ToDouble (arr [2], nfi);
						}
						catch{
							invalidStringCounter++;
							continue;
						}
						if (!IsValidPhrase (srcTextWCap, sourceLang, trgTextWCap, targetLang)) {
							AppendToErr (srcTextWCap, trgTextWCap, prob, swe, "INVALID_STRING_FILTER");
							invalidStringCounter++;
							continue;
						}

						if (!dict.ContainsKey (srcTextWCap))
							dict.Add (srcTextWCap, new Dictionary<string, double>());
						if (!dict [srcTextWCap].ContainsKey (trgTextWCap))
							dict [srcTextWCap].Add (trgTextWCap, prob);

						string srcStem = stemDictionary [sourceLang].ContainsKey (srcText) ? stemDictionary [sourceLang] [srcText] : srcStemmer.StemWord (srcText);
						if (!stemDictionary [sourceLang].ContainsKey (srcText))
							stemDictionary [sourceLang].Add (srcText, srcStem);
						string trgStem = stemDictionary [targetLang].ContainsKey (trgText) ? stemDictionary [targetLang] [trgText] : trgStemmer.StemWord (trgText);
						if (!stemDictionary [targetLang].ContainsKey (trgText))
							stemDictionary [targetLang].Add (trgText, trgStem);

						string srcSimple = SimpleCharacterTransliteration.Transliterate(srcStem);
						string trgSimple = SimpleCharacterTransliteration.Transliterate(trgStem);

						if (!srcToTrgStems.ContainsKey (srcStem))
							srcToTrgStems.Add (srcStem, new Dictionary<string, double> ());
						if (!srcToTrgStems [srcStem].ContainsKey (trgStem)) {
							double levenshtainDistance = LevenshteinDistance.Compute (srcSimple, trgSimple);
							double maxLen = Math.Max (srcSimple.Length, trgSimple.Length);
							double similarity = (maxLen - levenshtainDistance) / maxLen;
							srcToTrgStems [srcStem].Add (trgStem, similarity);
						}
                        //If word pairs are valid and the words contain at least one letter, we add them to the "to be transliterated" list.
						if (!srcWordsForTranslit.ContainsKey (srcText) && srcText.IndexOfAny(ValidAlphabets.GetAlphabet(sourceLang).ToCharArray())>=0)
							srcWordsForTranslit.Add (srcText,true);
                        if (!trgWordsForTranslit.ContainsKey(trgText) && trgText.IndexOfAny(ValidAlphabets.GetAlphabet(targetLang).ToCharArray()) >= 0)
							trgWordsForTranslit.Add (trgText, true);
					}
				}
			}
			sr.Close ();
			Console.WriteLine ("Reading of the dictionary file done!");

			Console.WriteLine("Translitrating words...");
			TransliterationModule tm = new TransliterationModule ();

			Dictionary<string,List<StringProbabEntry>> translitSrcDict = new Dictionary<string, List<StringProbabEntry>> ();
			Dictionary<string,List<StringProbabEntry>> translitTrgDict = new Dictionary<string, List<StringProbabEntry>> ();

			if (useHeavyTranslit) {
				if (!File.Exists (srcTranslitBackup)) {
					translitSrcDict = tm.GetTransliterations (srcWordsForTranslit, sourceLang, targetLang, mosesPath, tempFilePath, 6);
					SaveBackupTranslits (srcTranslitBackup, translitSrcDict);
				} else {
					translitSrcDict = LoadBackupTranslits (srcTranslitBackup);
				}
				if (!File.Exists (trgTranslitBackup)) {
					translitTrgDict = tm.GetTransliterations (trgWordsForTranslit, targetLang, sourceLang, mosesPath, tempFilePath, 6);
					SaveBackupTranslits (trgTranslitBackup, translitTrgDict);
				} else {
					translitTrgDict = LoadBackupTranslits (trgTranslitBackup);
				}
			}

			Console.WriteLine(" done!");

			double translCounter = 0;
			double idfFilterCounter = 0;
			double translitFilterCounter = 0;
			double heuristicFilterCounter = 0;
			double uniqueCounter = 0;
			double writtenCounter = 0;
			double absThresholdCounter = 0;
			double secondThresholdFilterCounter = 0;

			Console.WriteLine ("Analysing dictionary entries...");

			counter = 0;
			foreach (string srcWord in dict.Keys) {
				counter++;
				if (counter % 10000 == 0) {
					Console.Error.Write (".");
					if (counter % 500000 == 0) {
						Console.Error.WriteLine (" - " + counter.ToString ());
					}
				}
				string srcLower = srcWord.ToLower ();
				string srcStem = stemDictionary [sourceLang] [srcLower];
				double srcIdf = srcIdfDict.ContainsKey (srcStem) ? srcIdfDict [srcStem] : maxSrcIdf;

				//At first, try to identify whether something can be paired using transliteration. If yes, apply to all other pairs a higher threshold (0.4).
				//If no transliteration can be found, apply the minimum IDF threshold.
				//Allow falling through pairs with only one translation candidate

				List<StringProbabEntry> foundTranslits = new List<StringProbabEntry> ();
				List<StringProbabEntry> validAfterMinimumIdf = new List<StringProbabEntry> ();
				Dictionary<string,double> maxStemDict = new Dictionary<string, double> ();
				Dictionary<string,int> maxStemCountDict = new Dictionary<string, int> ();
				double maxProb = 0;
				int maxStemCount = 0;

				foreach (string trgWord in dict[srcWord].Keys) {
					string trgLower = trgWord.ToLower ();
					string trgStem = stemDictionary [targetLang] [trgLower];
					double prob = dict [srcWord] [trgWord];
					double simpleTranslitScore = srcToTrgStems [srcStem] [trgStem];

                    List<StringProbabEntry> srcTranslits = translitSrcDict.ContainsKey(srcLower) ? translitSrcDict[srcLower] : null;
                    if (srcTranslits != null)
                    {
                        for (int i = 0; i < srcTranslits.Count; i++)
                        {
                            if (string.IsNullOrWhiteSpace(srcTranslits[i].stem))
                            {
                                srcTranslits[i].stem = stemDictionary[targetLang].ContainsKey(srcTranslits[i].str) ? stemDictionary[targetLang][srcTranslits[i].str] : trgStemmer.StemWord(srcTranslits[i].str);
                                if (!stemDictionary[targetLang].ContainsKey(srcTranslits[i].str)) stemDictionary[targetLang].Add(srcTranslits[i].str, srcTranslits[i].stem);
                            }
                        }
                    }
                    List<StringProbabEntry> trgTranslits = translitTrgDict.ContainsKey(trgLower) ? translitTrgDict[trgLower] : null;
                    if (trgTranslits != null)
                    {
                        for (int i = 0; i < trgTranslits.Count; i++)
                        {
                            if (string.IsNullOrWhiteSpace(trgTranslits[i].stem))
                            {
                                trgTranslits[i].stem = stemDictionary[sourceLang].ContainsKey(trgTranslits[i].str) ? stemDictionary[sourceLang][trgTranslits[i].str] : srcStemmer.StemWord(trgTranslits[i].str);
                                if (!stemDictionary[sourceLang].ContainsKey(trgTranslits[i].str)) stemDictionary[sourceLang].Add(trgTranslits[i].str, trgTranslits[i].stem);
                            }
                        }
                    }
                    double maxSrcTranslitScore = srcTranslits != null ? GetMaxTranslitScore(trgStem.ToLower(), srcTranslits) : 0;
                    double maxTrgTranslitScore = trgTranslits != null ? GetMaxTranslitScore(srcStem.ToLower(), trgTranslits) : 0;

                    double minLen = Math.Min(srcLower.Length, trgLower.Length);
                    double maxLen = Math.Max(srcLower.Length, trgLower.Length);

                    //Check whether the words may be transliterations (but only if they are not equal!
                    if (srcLower != trgLower && srcLower.Length >= 4 && trgLower.Length >= 4 && minLen / maxLen > 0.6)
                    {
						double maxTranslScore = Math.Max (simpleTranslitScore, Math.Max (maxSrcTranslitScore, maxTrgTranslitScore));
						if (maxTranslScore >= 0.7){// || (maxTranslScore >= 0.6 && (sourceLang == "el"||targetLang == "el")) || (maxTranslScore >= 0.6 && (sourceLang == "ru"||targetLang == "ru"))) {
							foundTranslits.Add (new StringProbabEntry (trgWord, prob));
                            if (IsValidPhrase(srcWord, sourceLang) && IsValidPhrase(trgWord, targetLang) && Char.IsLetter(srcWord[0]) && Char.IsLetter(trgWord[0]))
                            {
                                swt.WriteLine(srcWord + "\t" + trgWord + "\t" + maxTranslScore.ToString(nfi));
                            }
                            continue;
						}
					}

					//Get the IDF score of the target and then check whether the IDF proportion is not too big (this deals only with stopwords).
					double trgIdf = trgIdfDict.ContainsKey (trgStem) ? trgIdfDict [trgStem] : maxTrgIdf;
					double trgAdjustedIdf = trgIdf * (maxSrcIdf / maxTrgIdf);

					double idfProp = Math.Min (srcIdf, trgAdjustedIdf) / Math.Max (srcIdf, trgAdjustedIdf);
					if (idfProp < idfPropThreshold){
						idfFilterCounter++;
                        AppendToErr(srcWord, trgWord, prob, swe, "IDF_FILTER");
						continue;
					}
					if (prob < absThreshold && dict [srcWord].Count > 1) {
						absThresholdCounter++;
						AppendToErr (srcWord, trgWord, prob, swe, "ABS_THRESHOLD_FILTER");
						continue;
					}
					validAfterMinimumIdf.Add (new StringProbabEntry (trgWord, prob,idfProp,trgStem));
					if (!maxStemDict.ContainsKey (trgStem))
						maxStemDict.Add (trgStem, prob);
					if (maxStemDict [trgStem] < prob)
						maxStemDict [trgStem] = prob;
					if (maxProb < prob)
						maxProb = prob;
					if (!maxStemCountDict.ContainsKey (trgStem))
						maxStemCountDict.Add (trgStem, 1);
					else
						maxStemCountDict [trgStem]++;
					if (maxStemCountDict [trgStem] > maxStemCount) {
						maxStemCount = maxStemCountDict [trgStem];
					}
				}

				double updatedThr = foundTranslits.Count > 0 ? afterTranslThreshold : initialThreshold;
				foreach (StringProbabEntry spe in foundTranslits) {
					sw.Write (srcWord);
					sw.Write (" ");
					sw.Write (spe.str);
					sw.Write (" ");
                    sw.WriteLine(spe.probab.ToString(nfi));
					//sw.WriteLine (" TRANSLIT");
					translCounter++;
					writtenCounter++;
				}
				foreach (StringProbabEntry spe in validAfterMinimumIdf) {
					if (validAfterMinimumIdf.Count > 1) {
						if (spe.probab >= updatedThr) {
							bool valid = true;
							foreach (StringProbabEntry tre in foundTranslits) {
								if (spe.str.ToLower ().Contains (tre.str.ToLower ())) {
									valid = false;
									translitFilterCounter++;
									AppendToErr (srcWord, spe.str, spe.probab, swe, "TRANSLITERATION_FILTER");
                                    swte.WriteLine(srcWord + "\t" + spe.str, spe.probab.ToString(nfi));
									break;
								}
							}
							if (valid && ((spe.str == srcWord && foundTranslits.Count + validAfterMinimumIdf.Count > 3)
							    || (maxStemDict [spe.stem] < (maxProb / 2))
							    || (maxStemCount >= 3 && maxStemCountDict [spe.stem] < 2)
							    || (spe.probab == 1 && maxStemCountDict [spe.stem] == 1 && foundTranslits.Count + validAfterMinimumIdf.Count >= 5))) {
								heuristicFilterCounter++;
								AppendToErr (srcWord, spe.str, spe.probab, swe, "HEURISTIC_FILTER");
								valid = false;
							}
							if (valid) {
								sw.Write (srcWord);
								sw.Write (" ");
								sw.Write (spe.str);
								sw.Write (" ");
                                sw.WriteLine(spe.probab.ToString(nfi));
								/*sw.Write (" OVER_THRESHOLD");
								sw.Write (" ");
								sw.Write (spe.idf);
								sw.Write (" ");
								sw.Write (spe.stem);
								sw.Write (" ");
								sw.WriteLine (maxStemDict[spe.stem]);*/
								writtenCounter++;
							}
						} else {
							secondThresholdFilterCounter++;
							AppendToErr (srcWord, spe.str, spe.probab, swe, "SECOND_THRESHOLD_FILTER");
						}
					} else if (foundTranslits.Count == 0) {
						sw.Write (srcWord);
						sw.Write (" ");
						sw.Write (spe.str);
						sw.Write (" ");
                        sw.WriteLine(spe.probab.ToString(nfi));
						//sw.WriteLine (" UNIQUE");
						uniqueCounter++;
						writtenCounter++;
					}
				}
			}
			Console.WriteLine ("Analysis of dictionary entries done!");
			Console.WriteLine ("============= Summary ==============");
			Console.Write ("Total entries: ");
			Console.WriteLine (totalCounter);
			Console.Write ("Invalid char pairs: ");
			Console.Write (invalidStringCounter);
			double proc = invalidStringCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("IDF filtered pairs: ");
			Console.Write (idfFilterCounter);
			proc = idfFilterCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("Absolute threshold filtered pairs: ");
			Console.Write (absThresholdCounter);
			proc = absThresholdCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("Second threshold filtered pairs: ");
			Console.Write (secondThresholdFilterCounter);
			proc = secondThresholdFilterCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("Transliteration filtered pairs: ");
			Console.Write (translitFilterCounter);
			proc = translitFilterCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("Heuristic filtered pairs: ");
			Console.Write (heuristicFilterCounter);
			proc = heuristicFilterCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("Transliterations: ");
			Console.Write (translCounter);
			proc = translCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("Pairs with only one translation: ");
			Console.Write (uniqueCounter);
			proc = uniqueCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.Write ("Total written pairs: ");
			Console.Write (writtenCounter);
			proc = writtenCounter / totalCounter * 100;
			Console.WriteLine (" ({0:#.##}%)", proc);
			Console.WriteLine ("====================================");
			sw.Close ();
			swe.Close ();
            swt.Close();
            swte.Close();
		}

		static double GetMaxTranslitScore (string str, List<StringProbabEntry> list)
		{
			double maxSim = 0;
			foreach (StringProbabEntry spe in list) {
				double levenshtainDistance = LevenshteinDistance.Compute (str, spe.stem);
				double maxLen = Math.Max (str.Length, spe.stem.Length);
				double similarity = (maxLen - levenshtainDistance) / maxLen;
				if (similarity > maxSim)
					maxSim = similarity;
			}
			return maxSim;
		}

		static double GetMaxIdf (Dictionary<string, double> srcIdfDict)
		{
			double max = Double.MinValue;
			foreach (string t in srcIdfDict.Keys) {
				if (srcIdfDict [t] > max)
					max = srcIdfDict [t];
			}
			return max;
		}

		private static bool IsValidPhrase(string src, string srcLang, string trg, string trgLang)
		{
			string srcLower = src.ToLower ();
			string trgLower = trg.ToLower ();
			if (IsValidCase (src, trg)) {
				if (IsValidPhrase (srcLower, srcLang) && IsValidPhrase (trgLower, trgLang)) {
					return true;
				}
				if (IsValidPhrase (srcLower, srcLang, true) && IsValidPhrase (trgLower, trgLang, true) && IsSameShape (srcLower, trgLower) && IsRightNumericCount (srcLower, trgLower) && IsRightPunctuationCount (srcLower, trgLower) && IsRightSymbolCount (srcLower, trgLower)) {
					return true;
				}
			}
			return false;
		}

		static bool IsValidCase (string src, string trg)
		{
			bool srcAllLower = true;
			bool trgAllLower = true;
			bool srcAllUpper = true;
			bool trgAllUpper = true;
			bool srcFirstUpper = false;
			bool trgFirstUpper = false;
			bool srcMixed = false;
			bool trgMixed = false;

			bool wasLower = false;
			bool wasUpper = false;
			for (int i=0;i<src.Length;i++) {
				if (Char.IsLetter (src [i])) {
					if (Char.IsLower (src [i])) {
						srcAllUpper = false;
						wasLower = true;
					} else if (Char.IsUpper (src [i])) {
						srcAllLower = false;
						if (i == 0) {
							srcFirstUpper = true;
						} else {
							wasUpper = true;
							srcFirstUpper = false;
						}
					}
				}
			}
			if (wasLower && wasUpper)
				srcMixed = true;

			wasLower = false;
			wasUpper = false;
			for (int i=0;i<trg.Length;i++) {
				if (Char.IsLetter (trg [i])) {
					if (Char.IsLower (trg [i])) {
						trgAllUpper = false;
						wasLower = true;
					} else if (Char.IsUpper (trg [i])) {
						trgAllLower = false;
						if (i == 0) {
							trgFirstUpper = true;
						} else {
							wasUpper = true;
							trgFirstUpper = false;
						}
					}
				}
			}
			if (wasLower && wasUpper)
				trgMixed = true;

			if (srcAllLower && trgAllLower || srcAllUpper && trgAllUpper || srcFirstUpper && trgFirstUpper || srcMixed && trgMixed)
				return true;
			//For some languages nouns are written in title case, therefore, allow some mix-up of cases.
			if (srcFirstUpper && trgAllLower || trgFirstUpper && srcAllLower)
				return true;
			return false;
		}

		static bool IsSameShape (string src, string trg)
		{
			string srcShape = GetShape (src);
			string trgShape = GetShape (trg);
			return srcShape == trgShape;
		}

		static string GetShape (string word)
		{
			char prev = '\n';
			StringBuilder sb = new StringBuilder ();
			foreach (char c in word) {
				if (Char.IsDigit (c) && prev != 'D') {
					sb.Append ('D');
					prev='D';
				}
				else if (Char.IsPunctuation (c) && prev != 'P') {
					sb.Append ('P');
					prev='P';
				}
				else if (Char.IsLetter (c) && prev != 'L') {
					sb.Append ('L');
					prev='L';
				}
				else if (Char.IsSymbol (c) && prev != 'S') {
					sb.Append ('S');
					prev='S';
				}
				else if (Char.IsWhiteSpace (c) && prev != 'W') {
					sb.Append ('W');
					prev='W';
				}
				else if (Char.IsNumber (c) && prev != 'N') {
					sb.Append ('N');
					prev='N';
				}
			}
			return sb.ToString ();
		}

		static bool IsRightNumericCount (string src, string trg)
		{
			Dictionary<char,int> numericCount = new Dictionary<char, int> ();
			foreach (char c in src) {
				if (Char.IsDigit (c)) {
					if (!numericCount.ContainsKey (c))
						numericCount.Add (c, 1);
					else
						numericCount [c]++;
				}
			}
			foreach (char c in trg) {
				if (Char.IsDigit (c)) {
					if (!numericCount.ContainsKey (c))
						return false;
					else
						numericCount [c]--;
				}
			}
			foreach (char c in numericCount.Keys) {
				if (numericCount [c] > 0)
					return false;
			}
			return true;
		}

		static bool IsRightPunctuationCount (string src, string trg)
		{
			Dictionary<char,int> punctCount = new Dictionary<char, int> ();
			foreach (char c in src) {
				if (Char.IsPunctuation (c)) {
					if (!punctCount.ContainsKey (c))
						punctCount.Add (c, 1);
					else
						punctCount [c]++;
				}
			}
			foreach (char c in trg) {
				if (Char.IsPunctuation (c)) {
					if (!punctCount.ContainsKey (c))
						return false;
					else
						punctCount [c]--;
				}
			}
			foreach (char c in punctCount.Keys) {
				if (punctCount [c] > 0)
					return false;
			}
			return true;
		}

		static bool IsRightSymbolCount (string src, string trg)
		{
			Dictionary<char,int> symbolCount = new Dictionary<char, int> ();
			foreach (char c in src) {
				if (Char.IsSymbol (c)) {
					if (!symbolCount.ContainsKey (c))
						symbolCount.Add (c, 1);
					else
						symbolCount [c]++;
				}
			}
			foreach (char c in trg) {
				if (Char.IsSymbol (c)) {
					if (!symbolCount.ContainsKey (c))
						return false;
					else
						symbolCount [c]--;
				}
			}
			foreach (char c in symbolCount.Keys) {
				if (symbolCount [c] > 0)
					return false;
			}
			return true;
		}

		static Dictionary<string, bool> warningsPrinted = new Dictionary<string, bool>();
		private static bool IsValidPhrase(string p, string language, bool acceptSymbolsAndOtherGarbage = false)
		{
			string baseAlphabet = "";
			switch (language.ToUpper ()) {
			case "BG":
				baseAlphabet = ValidAlphabets.BG;
				break;
			case "CS":
				baseAlphabet = ValidAlphabets.CS;
				break;
			case "CA":
				baseAlphabet = ValidAlphabets.CA;
				break;
			case "CY":
				baseAlphabet = ValidAlphabets.CY;
				break;
			case "DA":
				baseAlphabet = ValidAlphabets.DA;
				break;
			case "DE":
				baseAlphabet = ValidAlphabets.DE;
				break;
			case "EL":
				baseAlphabet = ValidAlphabets.EL;
				break;
			case "EN":
				baseAlphabet = ValidAlphabets.EN;
				break;
			case "ES":
				baseAlphabet = ValidAlphabets.ES;
				break;
			case "ET":
				baseAlphabet = ValidAlphabets.ET;
				break;
			case "EU":
				baseAlphabet = ValidAlphabets.EU;
				break;
			case "FI":
				baseAlphabet = ValidAlphabets.FI;
				break;
			case "FR":
				baseAlphabet = ValidAlphabets.FR;
				break;
			case "GA":
				baseAlphabet = ValidAlphabets.GA;
				break;
			case "GD":
				baseAlphabet = ValidAlphabets.GD;
				break;
			case "GL":
				baseAlphabet = ValidAlphabets.GL;
				break;
			case "HR":
				baseAlphabet = ValidAlphabets.HR;
				break;
			case "HU":
				baseAlphabet = ValidAlphabets.HU;
				break;
			case "HI":
				baseAlphabet = ValidAlphabets.HI;
				break;
			case "IT":
				baseAlphabet = ValidAlphabets.IT;
				break;
			case "LT":
				baseAlphabet = ValidAlphabets.LT;
				break;
			case "LV":
				baseAlphabet = ValidAlphabets.LV;
				break;
			case "MT":
				baseAlphabet = ValidAlphabets.MT;
				break;
			case "NL":
				baseAlphabet = ValidAlphabets.NL;
				break;
			case "PL":
				baseAlphabet = ValidAlphabets.PL;
				break;
			case "PT":
				baseAlphabet = ValidAlphabets.PT;
				break;
			case "RO":
				baseAlphabet = ValidAlphabets.RO;
				break;
			case "RU":
				baseAlphabet = ValidAlphabets.RU;
				break;
			case "SK":
				baseAlphabet = ValidAlphabets.SK;
				break;
			case "SL":
				baseAlphabet = ValidAlphabets.SL;
				break;
			case "SV":
				baseAlphabet = ValidAlphabets.SV;
				break;
			case "TR":
				baseAlphabet = ValidAlphabets.TR;
				break;
			case "UR":
				baseAlphabet = ValidAlphabets.UR;
				break;
			}
			if (!warningsPrinted.ContainsKey ("phraseValidation") && string.IsNullOrWhiteSpace (baseAlphabet)) {
				warningsPrinted.Add ("phraseValidation", true);
				Console.Error.WriteLine ("Phrase validation not supported for the language " + language);
				throw new ArgumentException ("Phrase validation not supported for the language " + language);
				//Console.Error.WriteLine("Using default EN instead");
			}
			int spaceCount = 0;
			int punctCount = 0;
			bool wasNonSpace = false;
			bool onlyPunct = true;
			foreach (char c in p) {
				if (wasNonSpace && Char.IsWhiteSpace (c))
					spaceCount++;
				else
					wasNonSpace = true;
				if (Char.IsPunctuation (c))
					punctCount++;
				if (!acceptSymbolsAndOtherGarbage && baseAlphabet.IndexOf (c) < 0 && ValidAlphabets.punctuationsForTermsPhraseTableFiltering.IndexOf (c) < 0) {//&& ValidAlphabets.EN.IndexOf(c) < 0)
					return false;
				} else if (acceptSymbolsAndOtherGarbage && !Char.IsDigit (c) && !Char.IsNumber (c) && !Char.IsPunctuation (c) && !Char.IsSymbol (c)) {
					return false;
				} else if (Char.IsLetter (c)) {
					onlyPunct = false;
				}
			}
			if (spaceCount > 0 || punctCount > 1 || (!acceptSymbolsAndOtherGarbage && onlyPunct)) {
				return false;
			}
			return true;
		}

		public static Stemmer GetStemmer(string lang)
		{
			switch (lang)
			{
			case "en": return new EngilshStemmer(); // new EngilshStemmer();
			case "es": return new SpanishStemmer();
			case "et": return new EstonianStemmer();
			case "fi": return new FinnishStemmer();
			case "fr": return new FrenchStemmer();
			case "ga": return new IrishStemmer();
			case "hr": return new CroatianStemmer();
			case "hu": return new HungarianStemmer();
			case "it": return new ItalianStemmer();
			case "lt": return new LithuanianStemmer();
			case "lv": return new FullLatvianStemmer();
			case "mt": return new MalteseStemmer();
			case "nl": return new DutchStemmer();
			case "pl": return new PolishStemmer();
			case "pt": return new PortugueseStemmer();
			case "ro": return new RomanianStemmer();
			case "ru": return new RussianStemmer();
			case "sk": return new SlovakStemmer();
			case "sl": return new SlovenianStemmer();
			case "sv": return new SwedishStemmer();
			case "cs": return new CzechStemmer();
			case "bg": return new BulgarianStemmer();
			case "da": return new DanishStemmer();
			case "de": return new GermanStemmer();
			case "el": return new GreekStemmer();
			default: return new GenericStemmer();
			}
		}

		public static Dictionary<string, double> ParseIdfFile(string file, Encoding enc, Stemmer stemmer, string lang)
		{
			char[] sep = { '\t', ' '};
			Dictionary<string, double> res = new Dictionary<string, double> ();
			StreamReader sr = new StreamReader(file, enc);
			while (!sr.EndOfStream) {
				string line = sr.ReadLine ().Trim ();
				try {
					if (!string.IsNullOrWhiteSpace (line)) {
						string[] arr = line.Split (sep, StringSplitOptions.RemoveEmptyEntries);
						if (arr.Length >= 2) {
							string tok = arr [0].Trim ().ToLower ();
							double prob = Convert.ToDouble (arr [1], nfi);
							string stem = "";
							if (stemDictionary [lang].ContainsKey (tok)) {
								stem = stemDictionary [lang] [tok];
							} else {
								stem = stemmer.StemWord (tok);
								stemDictionary [lang].Add (tok, stem);
							}
							if (!res.ContainsKey (stem)) {
								res.Add (stem, prob);
							} else {
								if (res [stem] > prob) {
									res [stem] = prob;
								}
							}
						}
					}
				} catch {
				}
			}
			sr.Close ();
			return res;
		}
	}
}
