//
//  StringProbabEntry.cs
//
//  Author:
//       Mārcis Pinnis <marcis.pinnis@gmail.com>
//
//  Copyright (c) 2013 Mārcis Pinnis
//
//  This program can be freely used only for scientific and educational purposes.
//
//  This program is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

using System;
using System.Collections.Generic;
using System.Text;

namespace FilterGizaDictionary
{
    public class StringProbabEntry
    {
        public string str;
		public string stem;
        public double probab;
		public double idf;
		public double maxStemProbab;
        public StringProbabEntry()
        {
            str="";
            probab = 0;
			idf = 0;
			maxStemProbab = 0;
		}
		public StringProbabEntry(string s, double p)
		{
			str=s;
			probab = p;
		}
		public StringProbabEntry(string s, double p, double i, string _stem)
		{
			str=s;
			probab = p;
			idf = i;
			stem = _stem;
		}
    }
}
