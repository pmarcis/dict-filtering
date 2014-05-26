using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildCrossValidationDataSetsAndScriptsForTranslitSMT
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
                Console.WriteLine("Usage:\n./BuildCrossValidationDataSetsAndScriptsForTranslitSMT.exe [translit file dir] [working dir] [moses dir] [lang model dir] [eval script dir]");
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
                int wordsPerFold = srcWords.Count / 10;

                List<List<string>> randomSrcTrainFolds = new List<List<string>>();
                List<List<string>> randomTrgTrainFolds = new List<List<string>>();
                List<List<string>> randomSrcTuneFolds = new List<List<string>>();
                List<List<string>> randomTrgTuneFolds = new List<List<string>>();
                List<List<string>> randomSrcEvalFolds = new List<List<string>>();
                List<List<string>> randomTrgEvalFolds = new List<List<string>>();
                GetRandomFolds(srcWords, trgWords, ref randomSrcTrainFolds, ref randomTrgTrainFolds, ref randomSrcTuneFolds, ref randomTrgTuneFolds, ref randomSrcEvalFolds, ref randomTrgEvalFolds);
                
                string trainDataDir = langPairDir+"data"+Path.DirectorySeparatorChar.ToString();
                if (!Directory.Exists(trainDataDir)) Directory.CreateDirectory(trainDataDir);
                SaveFolds(srcLang, trgLang, trainDataDir, randomSrcTrainFolds, randomTrgTrainFolds, randomSrcTuneFolds, randomTrgTuneFolds, randomSrcEvalFolds, randomTrgEvalFolds);
                
                //we assume that monolingual language models have been already trained! This is because I already have IRSTLM language models for all languages!

                string mosesTrainingFile = langPairDir + "trainMoses.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
                string binariseModelsFile = langPairDir + "binariseModels.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
                string tuneModelsFile = langPairDir + "tuneModels.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
                string adjustIniFile = langPairDir + "adjustIniFiles.sh"; //We should create for each language pair one shell script (as this is a parallel process!)!
                string evalFile = langPairDir + "eval.sh";
                string evalTopNFile = langPairDir + "topNEval.sh";
                string distLimitFile = langPairDir + "adjustDistLimit.sh";
                string evalDir = langPairDir + "eval" + Path.DirectorySeparatorChar.ToString();
                StreamWriter swTrain = new StreamWriter(mosesTrainingFile, false, new UTF8Encoding(false));
                swTrain.NewLine = "\n";
                StreamWriter swBinarise = new StreamWriter(binariseModelsFile, false, new UTF8Encoding(false));
                swBinarise.NewLine = "\n";
                StreamWriter swTune = new StreamWriter(tuneModelsFile, false, new UTF8Encoding(false));
                swTune.NewLine = "\n";
                StreamWriter swIni = new StreamWriter(adjustIniFile, false, new UTF8Encoding(false));
                swIni.NewLine = "\n";
                StreamWriter swEval = new StreamWriter(evalFile, false, new UTF8Encoding(false));
                swEval.NewLine = "\n";
                StreamWriter swTopNEval = new StreamWriter(evalTopNFile, false, new UTF8Encoding(false));
                swTopNEval.NewLine = "\n";
                StreamWriter swDistLimit = new StreamWriter(distLimitFile, false, new UTF8Encoding(false));
                swDistLimit.NewLine = "\n";

                swTrain.WriteLine("#Train the translation model.");
                swBinarise.WriteLine("#Binarise translation and reordering models.");
                swTune.WriteLine("#Tune SMT systems.");
                swIni.WriteLine("#Create a copy of the moses.ini file and make it to use the binarized data.");

                swEval.Write("mkdir -p ");
                swEval.WriteLine(evalDir);
                
                for (int i = 0; i < randomSrcEvalFolds.Count; i++)
                {
                    string srcTuneFile = trainDataDir + "tune_" + i.ToString() + "." + srcLang;
                    string trgTuneFile = trainDataDir + "tune_" + i.ToString() + "." + trgLang;
                    string srcEvalFile = trainDataDir + "eval_" + i.ToString() + "." + srcLang;
                    string srcEvalSgmFile = evalDir + "eval_" + i.ToString() + "." + srcLang+".sgm";
                    string trgEvalFile = trainDataDir + "eval_" + i.ToString() + "." + trgLang;
                    string trgEvalSgmFile = evalDir + "eval_" + i.ToString() + "." + trgLang + ".sgm";
                    string evalOutFile = evalDir + "eval_" + i.ToString() + "." + trgLang + ".out";
                    string evalOutSgmFile = evalOutFile + ".sgm";
                    string nistBleuFile = evalDir + "eval_res_" + i.ToString() + ".nist-bleu";
                    
                    string trainFile = trainDataDir + "train_" + i.ToString();
                    string workingDir = langPairDir + "fold_" + i.ToString() + Path.DirectorySeparatorChar.ToString();
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
                    string adjustedWorkingDir = workingDir.Replace("/","\\/");
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

                    //Missing piece - adjustment of the reordering threshold to just 2.

                    swEval.Write("cd ");
                    swEval.WriteLine(workingDir);
                    swEval.Write(mosesDir);
                    swEval.Write("mosesdecoder/bin/moses -v 0 -f binarised-model.moses.ini < ");
                    swEval.Write(srcEvalFile);
                    swEval.Write(" > ");
                    swEval.WriteLine(evalOutFile);
                    swEval.Write("perl ");
                    swEval.Write(perlScriptDir);
                    swEval.Write("wrap-sgm.pl src ");
                    swEval.Write(srcLang);
                    swEval.Write(" ");
                    swEval.Write(trgLang);
                    swEval.Write(" < ");
                    swEval.Write(srcEvalFile);
                    swEval.Write(" > ");
                    swEval.WriteLine(srcEvalSgmFile);
                    swEval.Write("perl ");
                    swEval.Write(perlScriptDir);
                    swEval.Write("wrap-sgm.pl ref ");
                    swEval.Write(srcLang);
                    swEval.Write(" ");
                    swEval.Write(trgLang);
                    swEval.Write(" < ");
                    swEval.Write(trgEvalFile);
                    swEval.Write(" > ");
                    swEval.WriteLine(trgEvalSgmFile);
                    swEval.Write("perl ");
                    swEval.Write(perlScriptDir);
                    swEval.Write("wrap-xml.perl ");
                    swEval.Write(trgLang);
                    swEval.Write(" ");
                    swEval.Write(srcEvalSgmFile);
                    swEval.Write(" < ");
                    swEval.Write(evalOutFile);
                    swEval.Write(" > ");
                    swEval.WriteLine(evalOutSgmFile);
                    swEval.Write("perl ");
                    swEval.Write(perlScriptDir);
                    swEval.Write("mteval-v13a.pl -s ");
                    swEval.Write(srcEvalSgmFile);
                    swEval.Write(" -r ");
                    swEval.Write(trgEvalSgmFile);
                    swEval.Write(" -t ");
                    swEval.Write(evalOutSgmFile);
                    swEval.Write(" > ");
                    swEval.WriteLine(nistBleuFile);
                    swEval.WriteLine();

                    swDistLimit.Write("cd ");
                    swDistLimit.WriteLine(workingDir);
                    swDistLimit.Write("mono ");
                    swDistLimit.Write(perlScriptDir);
                    swDistLimit.WriteLine("AdjustDistortionLimit.exe train/model/moses.ini train/model/distortion-adjusted.moses.ini 2");

                    swTopNEval.Write("cd ");
                    swTopNEval.WriteLine(workingDir);
                    swTopNEval.Write(mosesDir);
                    swTopNEval.Write("mosesdecoder/bin/moses -v 0 -f binarised-model.moses.ini -i ");
                    swTopNEval.Write(srcEvalFile);
                    swTopNEval.Write(" -n-best-list ");
                    swTopNEval.Write(evalOutFile);
                    swTopNEval.WriteLine(".n-best 20"); //As equal results can be acquired, we need to consolidate them and keep just the unique ones later on.
                }
                swTrain.Close();
                swBinarise.Close();
                swTune.Close();
                swIni.Close();
                swEval.Close();
                swDistLimit.Close();
                swTopNEval.Close();
            }
        }

        private static void SaveFolds(string srcLang, string trgLang, string trainDataDir, List<List<string>> randomSrcTrainFolds, List<List<string>> randomTrgTrainFolds, List<List<string>> randomSrcTuneFolds, List<List<string>> randomTrgTuneFolds, List<List<string>> randomSrcEvalFolds, List<List<string>> randomTrgEvalFolds)
        {
            for (int i = 0; i < randomSrcEvalFolds.Count; i++)
            {
                string srcTrainFile = trainDataDir + "train_"+i.ToString()+"." + srcLang;
                string trgTrainFile = trainDataDir + "train_" + i.ToString() + "." + trgLang;
                string srcTuneFile = trainDataDir + "tune_" + i.ToString() + "." + srcLang;
                string trgTuneFile = trainDataDir + "tune_" + i.ToString() + "." + trgLang;
                string srcEvalFile = trainDataDir + "eval_" + i.ToString() + "." + srcLang;
                string trgEvalFile = trainDataDir + "eval_" + i.ToString() + "." + trgLang;

                WriteListToFile(randomSrcTrainFolds[i], srcTrainFile);
                WriteListToFile(randomTrgTrainFolds[i], trgTrainFile);
                WriteListToFile(randomSrcTuneFolds[i], srcTuneFile);
                WriteListToFile(randomTrgTuneFolds[i], trgTuneFile);
                WriteListToFile(randomSrcEvalFolds[i], srcEvalFile);
                WriteListToFile(randomTrgEvalFolds[i], trgEvalFile);
            }
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

        private static void GetRandomFolds(List<string> srcWords, List<string> trgWords, ref List<List<string>> randomSrcTrainFolds, ref List<List<string>> randomTrgTrainFolds, ref List<List<string>> randomSrcTuneFolds, ref List<List<string>> randomTrgTuneFolds, ref List<List<string>> randomSrcEvalFolds, ref List<List<string>> randomTrgEvalFolds)
        {
            int wordsPerFold = srcWords.Count / 10;
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
            for (int i = 0; i < 10; i++)
            {
                int tuneIndex = i;
                int evalIndex = (i + 1) % 10;
                int tuneLowerBound = tuneIndex * wordsPerFold;
                int tuneUpperBound = tuneIndex * wordsPerFold + wordsPerFold - 1;
                int evalLowerBound = evalIndex * wordsPerFold;
                int evalUpperBound = evalIndex * wordsPerFold + wordsPerFold - 1;

                List<string> srcTrainFold = new List<string>();
                List<string> trgTrainFold = new List<string>();
                List<string> srcTuneFold = new List<string>();
                List<string> trgTuneFold = new List<string>();
                List<string> srcEvalFold = new List<string>();
                List<string> trgEvalFold = new List<string>();

                for (int j = 0; j < srcRandom.Count; j++)
                {
                    if (j >= tuneLowerBound && j <= tuneUpperBound)
                    {
                        srcTuneFold.Add(srcRandom[j]);
                        trgTuneFold.Add(trgRandom[j]);
                    }
                    else if (j >= evalLowerBound && j <= evalUpperBound)
                    {
                        srcEvalFold.Add(srcRandom[j]);
                        trgEvalFold.Add(trgRandom[j]);
                    }
                    else
                    {
                        srcTrainFold.Add(srcRandom[j]);
                        trgTrainFold.Add(trgRandom[j]);
                    }
                }

                randomSrcTrainFolds.Add(srcTrainFold);
                randomTrgTrainFolds.Add(trgTrainFold);
                randomSrcTuneFolds.Add(srcTuneFold);
                randomTrgTuneFolds.Add(trgTuneFold);
                randomSrcEvalFolds.Add(srcEvalFold);
                randomTrgEvalFolds.Add(trgEvalFold);
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
                            if (!readPairs.ContainsKey(srcStr+trgStr) && !string.IsNullOrWhiteSpace(srcStr)&&!string.IsNullOrWhiteSpace(trgStr))
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
