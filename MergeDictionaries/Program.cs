using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergeDictionaries
{
    public class Program
    {
		static NumberFormatInfo nfi;
        static void Main(string[] args)
        {
			nfi = new NumberFormatInfo();
			nfi.CurrencyDecimalSeparator = ".";
			nfi.NumberDecimalSeparator = ".";
			nfi.PercentDecimalSeparator = ".";
            string inDirOne = args[0];
            string inDirTwo = args[1];
            string extOne = args[2];
            string extTwo = args[3];
            string outDir = args[4];

            if (!inDirOne.EndsWith(Path.DirectorySeparatorChar.ToString())) inDirOne += Path.DirectorySeparatorChar.ToString();
            if (!inDirTwo.EndsWith(Path.DirectorySeparatorChar.ToString())) inDirTwo += Path.DirectorySeparatorChar.ToString();
            if (!outDir.EndsWith(Path.DirectorySeparatorChar.ToString())) outDir += Path.DirectorySeparatorChar.ToString();
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            Console.WriteLine("FILE\tFile 1 pairs\tFile 1 source words\tFile 2 pairs\tFile 2 source words\tFile out pairs\t File out source words\tNew pairs\tNew source words");
                
            foreach (string fileOne in Directory.GetFiles(inDirOne, "*"+extOne, SearchOption.TopDirectoryOnly))
            {
                int fileOneCount = 0;
                int fileTwoCount = 0;
                int fileOutCount = 0;
                int fileOneSrcWords = 0;
                int fileTwoSrcWords = 0;
                int fileOutSrcWords = 0;
                int newSrcWords = 0;
                int newPairs = 0;
                string fileName = Path.GetFileName(fileOne);
                string outFile = outDir + fileName.Substring(0,fileName.Length-extOne.Length)+extTwo;
                string fileTwo = inDirTwo + fileName.Substring(0, fileName.Length - extOne.Length) + extTwo;
                if (File.Exists(fileTwo))
                {
                    Dictionary<string, Dictionary<string, double>> fileOneData = ReadDict(fileOne, ref fileOneCount);
                    fileOneSrcWords = fileOneData.Count;
                    Dictionary<string, Dictionary<string, double>> fileTwoData = ReadDict(fileTwo,ref fileTwoCount);
                    fileTwoSrcWords = fileTwoData.Count;
                    
                    foreach (string srcText in fileTwoData.Keys)
                    {
                        if (!fileOneData.ContainsKey(srcText))
                        {
                            newSrcWords++;
                            fileOneData.Add(srcText, new Dictionary<string, double>());
                        }
                        foreach (string trgText in fileTwoData[srcText].Keys)
                        {
                            if (!fileOneData[srcText].ContainsKey(trgText))
                            {
                                newPairs++;
                                fileOneData[srcText].Add(trgText, fileTwoData[srcText][trgText]);
                            }
                            else if (fileTwoData[srcText][trgText] > fileOneData[srcText][trgText])
                            {
                                fileOneData[srcText][trgText] = fileTwoData[srcText][trgText];
                            }
                        }
                    }

                    StreamWriter sw = new StreamWriter(outFile, false, new UTF8Encoding(false));
                    sw.NewLine = "\n";
                    List<string> srcWords = fileOneData.Keys.ToList();
                    srcWords.Sort();
                    foreach (string srcWord in srcWords)
                    {
                        fileOutSrcWords++;
                        List<KeyValuePair<string, double>> transls = fileOneData[srcWord].ToList();
                        transls.Sort(Compare);
                        foreach (KeyValuePair<string, double> kvp in transls)
                        {
                            fileOutCount++;
                            sw.Write(srcWord);
                            sw.Write("\t");
                            sw.Write(kvp.Key);
                            if (kvp.Value >= 0)
                            {
                                sw.Write("\t");
                                sw.Write(kvp.Value.ToString(nfi));
                            }
                            sw.WriteLine();
                        }
                    }
                    sw.Close();
                }
                else
                {
                    Console.WriteLine("[WARNING] File" + fileName + " does not exist in the second directory");
                    File.Copy(fileOne, outFile,true);
                    Dictionary<string, Dictionary<string, double>> fileOneData = ReadDict(fileOne, ref fileOneCount);
                    fileOneSrcWords = fileOneData.Count;
                    fileTwoCount = fileOneCount;
                    fileOutCount = fileOneCount;
                    fileTwoSrcWords = fileOneSrcWords;
                    fileOutSrcWords = fileOneSrcWords;
                    newPairs = 0;
                    newSrcWords = 0;
                }

                Console.Write(fileName);
                Console.Write("\t");
                Console.Write(fileOneCount);
                Console.Write("\t");
                Console.Write(fileOneSrcWords);
                Console.Write("\t");
                Console.Write(fileTwoCount);
                Console.Write("\t");
                Console.Write(fileTwoSrcWords);
                Console.Write("\t");
                Console.Write(fileOutCount);
                Console.Write("\t");
                Console.Write(fileOutSrcWords);
                Console.Write("\t");
                Console.Write(newPairs);
                Console.Write("\t");
                Console.WriteLine(newSrcWords);
            }
        }

        static int Compare(KeyValuePair<string, double> a, KeyValuePair<string, double> b)
        {
            return b.Value.CompareTo(a.Value);
        }

        private static Dictionary<string, Dictionary<string, double>> ReadDict(string file, ref int lineCount)
        {
            Dictionary<string, Dictionary<string, double>> res = new Dictionary<string,Dictionary<string,double>>();
            char[] sep = {'\t'};
            StreamReader sr = new StreamReader(file, Encoding.UTF8);
			//Console.WriteLine("Reading dictionary file "+Path.GetFileName(file));
            lineCount=0;
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    string[] arr = line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    if (arr != null && arr.Length >= 2)
                    {
                        string srcText = arr[0].Trim();
                        string trgText = arr[1].Trim();
                        if (string.IsNullOrWhiteSpace(srcText) || string.IsNullOrWhiteSpace(trgText)) continue;
                        double prob = -1;
                        try
                        {
                            if (arr.Length>=3)
                                prob = Convert.ToDouble(arr[2].Replace(",","."), nfi);
                        }
                        catch
                        {
                            continue;
                        }
                        if (!res.ContainsKey(srcText)) res.Add(srcText, new Dictionary<string, double>());
                        if (!res[srcText].ContainsKey(trgText)) res[srcText].Add(trgText, prob);
                        lineCount++;
                    }
                }
            }
            return res;
        }
    }
}
