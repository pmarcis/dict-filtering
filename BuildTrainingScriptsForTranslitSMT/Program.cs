using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTrainingScriptsForTranslitSMT
{
    class Program
    {
        static void Main(string[] args)
        {
            string inDir = args[0];
            string outDir = args[1];
            string mosesDir = args[2];
            string langModelDir = args[3];
            string perlScriptDir = args[4];
            if (string.IsNullOrWhiteSpace(inDir) || string.IsNullOrWhiteSpace(outDir) || string.IsNullOrWhiteSpace(mosesDir) || string.IsNullOrWhiteSpace(langModelDir) || string.IsNullOrWhiteSpace(perlScriptDir)
                || !Directory.Exists(inDir) || !Directory.Exists(mosesDir) || !Directory.Exists(langModelDir) || !Directory.Exists(perlScriptDir))
            {
                Console.WriteLine("Usage:\n./BuildCrossValidationDataSetsAndScriptsForTranslitSMT.exe [translit file dir] [working dir] [moses dir] [lang model dir] [distortion adjuster dir]");
                return;
            }
            string ext = "translit";
            double thr = 0.9;
            if (!outDir.EndsWith(Path.DirectorySeparatorChar.ToString())) outDir += Path.DirectorySeparatorChar.ToString();
            if (!mosesDir.EndsWith(Path.DirectorySeparatorChar.ToString())) mosesDir += Path.DirectorySeparatorChar.ToString();
            if (!langModelDir.EndsWith(Path.DirectorySeparatorChar.ToString())) langModelDir += Path.DirectorySeparatorChar.ToString();
            if (!perlScriptDir.EndsWith(Path.DirectorySeparatorChar.ToString())) perlScriptDir += Path.DirectorySeparatorChar.ToString();
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            string mosesTrainingFile = outDir + "trainMoses.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
            string binariseModelsFile = outDir + "binariseModels.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
            string tuneModelsFile = outDir + "tuneModels.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
            string adjustIniFile = outDir + "adjustIniFiles.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
            string distLimitFile = outDir + "adjustDistLimit.sh";
            StreamWriter swTrain = new StreamWriter(mosesTrainingFile, false, new UTF8Encoding(false));
            swTrain.NewLine = "\n";
            StreamWriter swBinarise = new StreamWriter(binariseModelsFile, false, new UTF8Encoding(false));
            swBinarise.NewLine = "\n";
            StreamWriter swTune = new StreamWriter(tuneModelsFile, false, new UTF8Encoding(false));
            swTune.NewLine = "\n";
            StreamWriter swIni = new StreamWriter(adjustIniFile, false, new UTF8Encoding(false));
            swIni.NewLine = "\n";
            StreamWriter swDistLimit = new StreamWriter(distLimitFile, false, new UTF8Encoding(false));
            swDistLimit.NewLine = "\n";

            swTrain.WriteLine("#Train the translation model.");
            swBinarise.WriteLine("#Binarise translation and reordering models.");
            swTune.WriteLine("#Tune SMT systems.");
            swIni.WriteLine("#Create a copy of the moses.ini file and make it to use the binarized data.");

            foreach (string file in Directory.GetFiles(inDir, "*." + ext))
            {
                string fileName = Path.GetFileName(file);
                string srcLang = fileName.Substring(0, 2);
                string trgLang = fileName.Substring(3, 2);
                string langPairDir = outDir + srcLang + "_" + trgLang + Path.DirectorySeparatorChar.ToString();
                if (!Directory.Exists(langPairDir)) Directory.CreateDirectory(langPairDir);
                List<string> srcWords = new List<string>();
                List<string> trgWords = new List<string>();
                ReadDictionary(file, ref srcWords, ref trgWords, thr);

                List<string> randomSrcTrainData = new List<string>();
                List<string> randomTrgTrainData = new List<string>();
                List<string> randomSrcTuneData = new List<string>();
                List<string> randomTrgTuneData = new List<string>();
                GetRandomData(srcWords, trgWords, ref randomSrcTrainData, ref randomTrgTrainData, ref randomSrcTuneData, ref randomTrgTuneData);

                string trainDataDir = langPairDir + "data" + Path.DirectorySeparatorChar.ToString();
                if (!Directory.Exists(trainDataDir)) Directory.CreateDirectory(trainDataDir);
                SaveData(srcLang, trgLang, trainDataDir, randomSrcTrainData, randomTrgTrainData, randomSrcTuneData, randomTrgTuneData);

                //we assume that monolingual language models have been already trained! This is because I already have IRSTLM language models for all languages!

                string srcTuneFile = trainDataDir + "tune." + srcLang;
                string trgTuneFile = trainDataDir + "tune." + trgLang;

                string trainFile = trainDataDir + "train";
                string workingDir = langPairDir + "wd";
                //if (!Directory.Exists(workingDir)) Directory.CreateDirectory(workingDir);
                swTrain.Write("mkdir -p ");
                swTrain.WriteLine(workingDir);
                swTrain.Write("cd ");
                swTrain.WriteLine(workingDir);
                swTrain.Write(mosesDir);
                swTrain.Write("mosesdecoder/scripts/training/train-model.perl -external-bin-dir ");
                swTrain.Write(mosesDir);
                swTrain.Write("external-bin-dir -root-dir train -corpus ");
                swTrain.Write(trainFile);
                swTrain.Write(" -f ");
                swTrain.Write(srcLang);
                swTrain.Write(" -e ");
                swTrain.Write(trgLang);
                swTrain.Write(" -alignment grow-diag-final-and -reordering msd-bidirectional-fe -lm 0:5:");
                swTrain.Write(langModelDir);
                swTrain.Write("mono.blm.");
                swTrain.Write(trgLang);
                swTrain.Write(":8 >& training.out &");
                swTrain.WriteLine();

                swBinarise.Write("cd ");
                swBinarise.WriteLine(workingDir);
                swBinarise.WriteLine("mkdir -p binarised-model");
                swBinarise.Write(mosesDir);
                swBinarise.WriteLine("mosesdecoder/bin/processPhraseTable -ttable 0 0 train/model/phrase-table.gz -nscores 5 -out binarised-model/phrase-table");
                swBinarise.Write(mosesDir);
                swBinarise.WriteLine("mosesdecoder/bin/processLexicalTable -in train/model/reordering-table.wbe-msd-bidirectional-fe.gz -out binarised-model/reordering-table");
                swBinarise.WriteLine();

                swTune.Write("cd ");
                swTune.WriteLine(workingDir);
                swTune.Write(mosesDir);
                swTune.Write("mosesdecoder/scripts/training/mert-moses.pl ");
                swTune.Write(srcTuneFile);
                swTune.Write(" ");
                swTune.Write(trgTuneFile);
                swTune.Write(" ");
                swTune.Write(mosesDir);
                swTune.Write("mosesdecoder/bin/moses train/model/distortion-adjusted.moses.ini --mertdir ");
                swTune.Write(mosesDir);
                swTune.Write("mosesdecoder/bin/ &> mert.out &");
                swTune.WriteLine();

                swIni.Write("cd ");
                swIni.WriteLine(workingDir);
                swIni.WriteLine("cp mert-work/moses.ini binarised-model.moses.ini");
                swIni.Write("sed -i \'s/0 0 0 5 ");
                string adjustedWorkingDir = workingDir.Replace("/", "\\/");
                swIni.Write(adjustedWorkingDir);
                swIni.Write("train\\/model\\/phrase-table\\.gz/1 0 0 5 ");
                swIni.Write(adjustedWorkingDir);
                swIni.WriteLine("binarised-model\\/phrase-table/g\' binarised-model.moses.ini");

                swIni.Write("sed -i \'s/0-0 wbe-msd-bidirectional-fe-allff 6 ");
                swIni.Write(adjustedWorkingDir);
                swIni.Write("train\\/model\\/reordering-table\\.wbe-msd-bidirectional-fe\\.gz/0-0 wbe-msd-bidirectional-fe-allff 6 ");
                swIni.Write(adjustedWorkingDir);
                swIni.WriteLine("binarised-model\\/reordering-table/g\' binarised-model.moses.ini");
                swIni.WriteLine();

                swDistLimit.Write("cd ");
                swDistLimit.WriteLine(workingDir);
                swDistLimit.Write("mono ");
                swDistLimit.Write(perlScriptDir);
                swDistLimit.WriteLine("AdjustDistortionLimit.exe train/model/moses.ini train/model/distortion-adjusted.moses.ini 2");
            }

            swTrain.Close();
            swBinarise.Close();
            swTune.Close();
            swIni.Close();
            swDistLimit.Close();
        }

        private static void SaveData(string srcLang, string trgLang, string trainDataDir, List<string> randomSrcTrainData, List<string> randomTrgTrainData, List<string> randomSrcTuneData, List<string> randomTrgTuneData)
        {
            string srcTrainFile = trainDataDir + "train." + srcLang;
            string trgTrainFile = trainDataDir + "train." + trgLang;
            string srcTuneFile = trainDataDir + "tune." + srcLang;
            string trgTuneFile = trainDataDir + "tune." + trgLang;
            string srcEvalFile = trainDataDir + "eval." + srcLang;
            string trgEvalFile = trainDataDir + "eval." + trgLang;

            WriteListToFile(randomSrcTrainData, srcTrainFile);
            WriteListToFile(randomTrgTrainData, trgTrainFile);
            WriteListToFile(randomSrcTuneData, srcTuneFile);
            WriteListToFile(randomTrgTuneData, trgTuneFile);
        }

        private static void WriteListToFile(List<string> data, string file)
        {
            StreamWriter sw = new StreamWriter(file, false, new UTF8Encoding(false));
            sw.NewLine = "\n";
            foreach (string s in data)
            {
                sw.WriteLine(s);
            }
            sw.Close();
        }

        private static void GetRandomData(List<string> srcWords, List<string> trgWords, ref List<string> randomSrcTrainData, ref List<string> randomTrgTrainData, ref List<string> randomSrcTuneData, ref List<string> randomTrgTuneData)
        {
            int tuneCount = srcWords.Count / 10 > 2000 ? 2000 : srcWords.Count / 10;
            Random r = new Random(DateTime.Now.Millisecond);
            List<string> srcRandom = new List<string>();
            List<string> trgRandom = new List<string>();

            while (srcWords.Count > 0)
            {
                int random = r.Next(0, srcWords.Count);
                srcRandom.Add(srcWords[random]);
                trgRandom.Add(trgWords[random]);
                srcWords.RemoveAt(random);
                trgWords.RemoveAt(random);
            }
            for (int j = 0; j < srcRandom.Count; j++)
            {
                if (j < tuneCount)
                {
                    randomSrcTuneData.Add(srcRandom[j]);
                    randomTrgTuneData.Add(trgRandom[j]);
                }
                else
                {
                    randomSrcTrainData.Add(srcRandom[j]);
                    randomTrgTrainData.Add(trgRandom[j]);
                }
            }
        }

        private static void ReadDictionary(string file, ref List<string> srcWords, ref List<string> trgWords, double thr)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.CurrencyDecimalSeparator = ".";
            nfi.NumberDecimalSeparator = ".";
            nfi.PercentDecimalSeparator = ".";

            char[] sep = { '\t' };

            Dictionary<string, bool> readPairs = new Dictionary<string, bool>();
            StreamReader sr = new StreamReader(file, Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] arr = line.Split(sep, StringSplitOptions.None);
                    if (arr.Length == 3)
                    {
                        string srcStr = GetValidStr(arr[0]);
                        string trgStr = GetValidStr(arr[1]);
                        double prob = Convert.ToDouble(arr[2], nfi);
                        if (prob >= thr)
                        {
                            if (!readPairs.ContainsKey(srcStr + trgStr) && !string.IsNullOrWhiteSpace(srcStr) && !string.IsNullOrWhiteSpace(trgStr))
                            {
                                readPairs.Add(srcStr + trgStr, true);
                                srcWords.Add(srcStr);
                                trgWords.Add(trgStr);
                            }
                        }
                    }
                    else if (arr.Length == 2)
                    {
                        string srcStr = GetValidStr(arr[0]);
                        string trgStr = GetValidStr(arr[1]);
                        if (!readPairs.ContainsKey(srcStr + trgStr) && !string.IsNullOrWhiteSpace(srcStr) && !string.IsNullOrWhiteSpace(trgStr))
                        {
                            readPairs.Add(srcStr + trgStr, true);
                            srcWords.Add(srcStr);
                            trgWords.Add(trgStr);
                        }
                    }
                }
            }
            sr.Close();
        }

        private static string GetValidStr(string p)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in p)
            {
                if (Char.IsWhiteSpace(c))
                {
                }
                else
                {
                    sb.Append(" ");
                    sb.Append(c);
                }
            }
            return sb.ToString().Trim();
        }
    }
}
