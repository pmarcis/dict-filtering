//
//  TransliterationModule.cs
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
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Globalization;

namespace FilterGizaDictionary
{
	public class TransliterationModule
	{
		public Dictionary<string,Dictionary<string,TranslConfig>> config = new Dictionary<string, Dictionary<string, FilterGizaDictionary.TranslConfig>>();

		public TransliterationModule ()
		{
			config = new Dictionary<string, Dictionary<string, TranslConfig>> ();
            string[] langs = { "LV", "LT", "ET", "BG", "CS", "DA", "DE", "EL", "FI", "HR", "MT", "PL", "RO", "RU", "SK", "SL", "SV", "EN", "ES", "IT", "FR", "DE", "NL", "GA", "PT", "HU" };
			foreach(string lang in langs)
			{
				string lower = lang.ToLower ();
				string upper = lang.ToUpper ();
                //TODO: The transliteration system path is hardcoded. Be aware! Possibly change it (if time allows)!
                //TranslConfig tc = new TranslConfig(lower, "en", "/home/marcis/TILDE/RESOURCES/TRANSLIT_WORKING_DIR_V2/" + upper + "-EN/" + lower + "-en-binarised-model.moses.ini");
                TranslConfig tc = new TranslConfig(lower, "en", "/home/marcis/TILDE/RESOURCES/TRANSLIT_SYSTEMS_4TH_ITERATION/" + lower + "_en/wd/binarised-model.moses.ini");
				if (!config.ContainsKey (lower))
					config.Add (lower, new Dictionary<string, TranslConfig> ());
				if (!config [lower].ContainsKey ("en"))
					config [lower].Add ("en", tc);

                //tc = new TranslConfig("en", lower, "/home/marcis/TILDE/RESOURCES/TRANSLIT_WORKING_DIR_V2/EN-" + upper + "/en-" + lower + "-binarised-model.moses.ini");
                tc = new TranslConfig("en", lower, "/home/marcis/TILDE/RESOURCES/TRANSLIT_SYSTEMS_4TH_ITERATION/en_" + lower + "/wd/binarised-model.moses.ini");
				if (!config.ContainsKey ("en"))
					config.Add ("en", new Dictionary<string, TranslConfig> ());
				if (!config ["en"].ContainsKey (lower))
					config ["en"].Add (lower, tc);
			}
		}
		public Dictionary<string, List<StringProbabEntry>> GetTransliterations (Dictionary<string,bool> lowerCasedTerms, string srcLang, string trgLang, string mosesPath, string tempFilePath, int threadCount)
        {
            Dictionary<string, List<StringProbabEntry>> res = new Dictionary<string, List<StringProbabEntry>> ();
			if (lowerCasedTerms == null || lowerCasedTerms.Count < 1 || string.IsNullOrWhiteSpace (srcLang)|| string.IsNullOrWhiteSpace (trgLang)|| string.IsNullOrWhiteSpace (mosesPath) || string.IsNullOrWhiteSpace (tempFilePath)) {
				return res;
			}
			TranslConfig tc = config [srcLang] [trgLang];
			string langKey = tc.srcLang + "_" + tc.trgLang;

            Log.Write ("Starting transliteration of " + lowerCasedTerms.Count.ToString () + " tokens.", LogLevelType.LIMITED_OUTPUT);
			int idx = 0;
			List<List<string>> lowerCasedTermDictList = new List<List<string>> (threadCount);
			for (int i=0; i<threadCount; i++) {
				lowerCasedTermDictList.Add (new List<string> ());
			}
			foreach (string term in lowerCasedTerms.Keys) {
				lowerCasedTermDictList [idx % threadCount].Add (term);
				idx++;
			}

            string directory = Path.GetDirectoryName (mosesPath);
			List<Process> processes = new List<Process> ();

			for (int i=0; i<lowerCasedTermDictList.Count; i++) {
				if (lowerCasedTermDictList [i].Count > 0) {
					try {
						string tmpFile = tempFilePath + i.ToString () + ".tmp";
						if (!File.Exists (tmpFile + ".n_best"))
						{
							WriteWordsForTransliteration (lowerCasedTermDictList [i], tmpFile);
							ProcessStartInfo myProcessStartInfo = new ProcessStartInfo (mosesPath);
							myProcessStartInfo.UseShellExecute = false;
							myProcessStartInfo.WorkingDirectory = directory;
							myProcessStartInfo.FileName = mosesPath;
							myProcessStartInfo.CreateNoWindow = true;
							myProcessStartInfo.RedirectStandardOutput = true;
							myProcessStartInfo.RedirectStandardError = true;

							StringBuilder sb = new StringBuilder ();
							sb.Append (" -f ");
							sb.Append ("\"" + tc.mosesPathIni + "\" ");
							sb.Append (" -i ");
							sb.Append ("\"" + tmpFile + "\" ");
							sb.Append (" -n-best-list ");
							sb.Append ("\"" + tmpFile + ".n_best\" " + tc.nBest.ToString ());
							myProcessStartInfo.Arguments = sb.ToString ();

							processes.Add (new Process ());
							processes [processes.Count - 1].StartInfo = myProcessStartInfo;
							bool started = processes [processes.Count - 1].Start ();
							processes [processes.Count - 1].ErrorDataReceived += p_ErrorDataReceived;
							processes [processes.Count - 1].OutputDataReceived += p_OutputDataReceived;
							processes [processes.Count - 1].BeginOutputReadLine ();
							processes [processes.Count - 1].BeginErrorReadLine ();
						}
					} catch (Exception ex) {
                        Console.Error.WriteLine(ex.ToString());
					}
				}
            }
            for (int i=0; i<processes.Count; i++) {
				processes [i].WaitForExit ();
				processes [i].Close ();
				processes [i].Dispose ();
			}
			processes.Clear ();

            
			for (int i=0; i<lowerCasedTermDictList.Count; i++) {
				if (lowerCasedTermDictList[i].Count > 0) {
					string tmpFile = tempFilePath + i.ToString () + ".tmp";
					if (File.Exists (tmpFile + ".n_best")) {

						NumberFormatInfo nfi = new NumberFormatInfo ();
						nfi.CurrencyDecimalSeparator = ".";
						nfi.NumberDecimalSeparator = ".";
						nfi.PercentDecimalSeparator = ".";
						Dictionary<string,Dictionary<string,bool>> existingTranslits = new Dictionary<string, Dictionary<string,bool>> ();

						StreamReader sr = new StreamReader (tmpFile + ".n_best", Encoding.UTF8);
						string[] sep = {"|||"};
						while (!sr.EndOfStream) {
							string line = sr.ReadLine ();
							string[] dataArr = line.Split (sep, StringSplitOptions.RemoveEmptyEntries);
							if (dataArr.Length == 4) {
								try {
									string idStr = dataArr [0];
									idStr = idStr.Trim ();
									int id = Convert.ToInt32 (idStr);
									string word = dataArr [1];

									StringProbabEntry spe = new StringProbabEntry ();
									spe.str = word.Trim ().Replace (" ", "");
									string probabStr = dataArr [3];
									probabStr = probabStr.Trim ().Replace (',', '.');
									spe.probab = Math.Exp (Convert.ToDouble (probabStr, nfi));
									if (spe.probab>1) spe.probab = 1;
									if (id < lowerCasedTermDictList[i].Count) {
										string term = lowerCasedTermDictList[i][id];
										double min = Math.Min (spe.str.Length, term.Length);
										double max = Math.Max (spe.str.Length, term.Length);
										double lenDiff = min / max;
										//Log.Write(term+" "+word+" "+lenDiff.ToString()+" "+spe.probab.ToString(),LogLevelType.ERROR);
										if (lenDiff >= tc.maxLenDiff) {
											if (!existingTranslits.ContainsKey (term))
												existingTranslits.Add (term, new Dictionary<string,bool> ());

											if (!res.ContainsKey (term))
												res.Add (term, new List<StringProbabEntry> ());
											if (!existingTranslits [term].ContainsKey (spe.str) && spe.probab >= tc.thr) {
												existingTranslits [term].Add (spe.str, true);
												res [term].Add (spe);
											}
										}
									}
								} catch {
								}
							}
						}
                        sr.Close();
					}
					try {
						File.Delete (tmpFile + ".n_best");
						File.Delete (tmpFile);
					} catch {
					}
				}
			}
			GC.Collect();
			GC.WaitForPendingFinalizers();
			return res;
		}

		static void p_OutputDataReceived (object sender, DataReceivedEventArgs e)
		{
			//throw new NotImplementedException ();
		}

		static void p_ErrorDataReceived (object sender, DataReceivedEventArgs e)
		{
			//throw new NotImplementedException ();
		}

		public static void WriteWordsForTransliteration(List<string> lowerCasedTerms, string outputFile)
		{
			if (string.IsNullOrWhiteSpace(outputFile)) return;
			Encoding outputEnc = new UTF8Encoding(false);
			StreamWriter sw = new StreamWriter(outputFile, false, outputEnc);
			sw.NewLine = "\n";
			foreach(string word in lowerCasedTerms)
			{
				foreach(char c in word)
				{
					sw.Write(c);
					sw.Write(" ");
				}
				sw.WriteLine();
			}
			sw.Close();
		}
	}
}

