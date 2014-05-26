using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidateEvalResults
{
    class Program
    {
        
        static NumberFormatInfo nfi = new NumberFormatInfo();

        static void Main(string[] args)
            {
                nfi.CurrencyDecimalSeparator = ".";
                nfi.NumberDecimalSeparator = ".";
                nfi.PercentDecimalSeparator = ".";
            string workingDir = args[0];
            string outFile = args[1];
            string method = args[2].ToUpper(); // BLEU, BLEU+NBEST, NBEST
            StreamWriter sw = new StreamWriter(outFile, false, new UTF8Encoding(false));

            foreach (string langPairDir in Directory.GetDirectories(workingDir, "*_*", SearchOption.TopDirectoryOnly))
            {
                string dir = langPairDir;
                if (!dir.EndsWith(Path.DirectorySeparatorChar.ToString())) dir += Path.DirectorySeparatorChar.ToString();
                string srcLang = langPairDir.Substring(dir.Length - 6, 2);
                string trgLang = langPairDir.Substring(dir.Length - 3, 2);
                Console.WriteLine("[CER] Processing language pair " + srcLang + "-" + trgLang);

                // NIST-BLEU eval file: eval/eval_res_0.nist-bleu
                // NBEST output file: eval/eval_0.en.out.n-best
                //NBEST reference file: data/eval_0.en
                if (method.Contains("BLEU"))
                {
                    AppendBleuEvaluation(sw, dir, srcLang, trgLang);
                }
                if (method.Contains("NBEST"))
                {
                    AppendNBestEvaluation(sw, dir, srcLang, trgLang);
                }

            }
            sw.Close();
        }

        private static void AppendNBestEvaluation(StreamWriter sw, string dir, string srcLang, string trgLang)
        {
            char[] sep = { ' ', '\t' };

            List<List<double>> crossValidationData = new List<List<double>>();
            for (int j = 0; j < 10; j++) crossValidationData.Add(new List<double>());

            for (int i = 0; i < 10; i++)
            {
                string refFile = dir + "data/eval_" + i.ToString() + "." + trgLang;
                string outFile = dir + "eval/eval_" + i.ToString() + "." + trgLang + ".out.n-best";
                if (!File.Exists(refFile) || !File.Exists(outFile)) return;
                List<string> refWords = ReadSimpleListFile(refFile);
                List<List<string>> outWords = ReadNBestListFile(outFile, refWords);
                //TOP 1, TOP 3, TOP 5, TOP 10
                List<double> correctWords = new List<double>();
                for (int j = 0; j < 10; j++) correctWords.Add(0);
                for (int j = 0; j < refWords.Count; j++)
                {
                    for (int k = 0; k < outWords[j].Count; k++)
                    {
                        if (k >= 10) break;
                        if (refWords[j] == outWords[j][k])
                        {
                            for (int l = k; l < 10; l++)
                                correctWords[l]++;
                            break;
                        }
                    }
                }
                double count = refWords.Count;
                for (int k = 0; k < 10; k++)
                {   
                    double prec = correctWords[k] / count;
                    crossValidationData[k].Add(prec);
                    sw.Write("NBEST-TOP-");
                    sw.Write(k+1);
                    sw.Write("\t");
                    sw.Write(srcLang);
                    sw.Write("\t");
                    sw.Write(trgLang);
                    sw.Write("\t");
                    sw.Write(i.ToString());
                    sw.Write("\t");
                    sw.Write(correctWords[k]);
                    sw.Write("\t");
                    sw.Write(count);
                    sw.Write("\t");
                    sw.WriteLine(prec.ToString(nfi));
                }
            }

            for (int k = 0; k < 10; k++)
            {
                ConfidenceInterval ci = new ConfidenceInterval(0.99, crossValidationData[k]);
                sw.Write("NBEST-X-VALIDATION-TOP-");
                sw.Write(k+1);
                sw.Write("\t");
                sw.Write(srcLang);
                sw.Write("\t");
                sw.Write(trgLang);
                sw.Write("\t");
                sw.Write(ci.Mean.ToString(nfi));
                sw.Write("\t");
                sw.Write(ci.MarginOfError.ToString(nfi));
                sw.Write("\t");
                sw.Write(ci.Percentage.ToString(nfi));
                sw.Write("\t");
                sw.Write(ci.Lower.ToString(nfi));
                sw.Write("\t");
                sw.WriteLine(ci.Upper.ToString(nfi));
            }
        }

        private static List<List<string>> ReadNBestListFile(string file, List<string> words)
        {
            List<List<string>> res = new List<List<string>>();
            for (int i = 0; i < words.Count; i++)
            {
                res.Add(new List<string>());
            }
            Dictionary<int, Dictionary<string, bool>> existingTranslits = new Dictionary<int, Dictionary<string, bool>>();
            StreamReader sr = new StreamReader(file, Encoding.UTF8);
            string[] sep = { "|||" };
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] dataArr = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if (dataArr.Length == 4)
                {
                    string idStr = dataArr[0];
                    idStr = idStr.Trim();
                    int id = Convert.ToInt32(idStr);
                    if (id >= res.Count) throw new InvalidDataException("Something is wrong! The ID should be less than the number of words");
                    string word = dataArr[1];
                    word = word.Trim().Replace(" ", "");

                    if (!existingTranslits.ContainsKey(id))
                        existingTranslits.Add(id, new Dictionary<string, bool>());
                    if (!existingTranslits[id].ContainsKey(word))
                    {
                        existingTranslits[id].Add(word, true);
                        res[id].Add(word);
                    }
                }
            }
            sr.Close();
            return res;
        }

        private static List<string> ReadSimpleListFile(string file)
        {
            List<string> res = new List<string>();
            StreamReader sr = new StreamReader(file, Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim().Replace(" ", "");
                res.Add(line);
            }
            sr.Close();
            return res;
        }

        private static void AppendBleuEvaluation(StreamWriter sw, string dir, string srcLang, string trgLang)
        {
            char[] sep = { ' ', '\t' };
            List<double> crossValidationBLEUData = new List<double>();
            List<double> crossValidationNISTData = new List<double>();


            for (int i = 0; i < 10; i++)
            {
                string file = dir + "eval" + Path.DirectorySeparatorChar.ToString() + "eval_res_" + i.ToString() + ".nist-bleu";
                if (File.Exists(file))
                {
                    StreamReader sr = new StreamReader(file, Encoding.UTF8);
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine().Trim();
                        if (line.StartsWith("NIST score"))
                        {
                            string[] arr = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            sw.Write("NIST-BLEU\t");
                            sw.Write(srcLang);
                            sw.Write("\t");
                            sw.Write(trgLang);
                            sw.Write("\t");
                            sw.Write(i.ToString());
                            sw.Write("\t");
                            double nistScore = Convert.ToDouble(arr[3], nfi);
                            sw.Write(nistScore.ToString(nfi));//NIST score
                            crossValidationNISTData.Add(nistScore);
                            sw.Write("\t");
                            double bleuScore = Convert.ToDouble(arr[7], nfi)*100.00;
                            crossValidationBLEUData.Add(bleuScore);
                            sw.WriteLine(bleuScore.ToString(nfi));//BLEU score
                            break;
                        }
                    }
                    sr.Close();
                }
                else
                {
                    return;
                }
            }

            ConfidenceInterval bleuCi = new ConfidenceInterval(0.99, crossValidationBLEUData);
            ConfidenceInterval nistCi = new ConfidenceInterval(0.99, crossValidationNISTData);
            sw.Write("NIST-BLEU-X-VALIDATION\t");
            sw.Write(srcLang);
            sw.Write("\t");
            sw.Write(trgLang);
            sw.Write("\t");
            sw.Write(nistCi.Mean.ToString(nfi));
            sw.Write("\t");
            sw.Write(nistCi.MarginOfError.ToString(nfi));
            sw.Write("\t");
            sw.Write(nistCi.Percentage.ToString(nfi));
            sw.Write("\t");
            sw.Write(nistCi.Lower.ToString(nfi));
            sw.Write("\t");
            sw.Write(nistCi.Upper.ToString(nfi));
            sw.Write("\t");
            sw.Write(bleuCi.Mean.ToString(nfi));
            sw.Write("\t");
            sw.Write(bleuCi.MarginOfError.ToString(nfi));
            sw.Write("\t");
            sw.Write(bleuCi.Percentage.ToString(nfi));
            sw.Write("\t");
            sw.Write(bleuCi.Lower.ToString(nfi));
            sw.Write("\t");
            sw.WriteLine(bleuCi.Upper.ToString(nfi));
            
        }
    }
}
