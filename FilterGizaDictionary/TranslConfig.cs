//
//  TranslConfig.cs
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

namespace FilterGizaDictionary
{
	public class TranslConfig
	{
		public TranslConfig ()
		{
		}

		public string srcLang="";
		public string trgLang="";
		public double thr=0.2;
		public double maxLenDiff = 0.6;
		public int nBest = 5;
		public string mosesPathIni = "";

		public TranslConfig(string sLang, string tLang, string mosesIni)
		{
			srcLang = sLang;
			trgLang = tLang;
			mosesPathIni = mosesIni;
		}
	}
}

