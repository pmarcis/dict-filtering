using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FilterGizaDictionary
{
    public abstract class Stemmer
    {
        public List<string> Stem(string text)
        {
            List<string> stemmed = new List<string>();
            int pos = -1;

            int i;
            for (i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    if (pos != -1 && i - pos > 0)
                    {
                        stemmed.Add(StemWord(text.Substring(pos, i - pos)));
                    }
                    pos = -1;
                }
                else
                {
                    if (pos == -1)
                        pos = i;
                }
            }

            if (pos != -1 && i - pos > 0)
            {
                stemmed.Add(StemWord(text.Substring(pos, i - pos)));
            }

            return stemmed;
        }

        public List<string> NGramStem(string text)
        {
            List<string> stemmed = new List<string>();
            int pos = -1;

            int i;
            for (i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    if (pos != -1 && i - pos > 0)
                    {
                        string stemmedWord = StemWord(text.Substring(pos, i - pos));
                        if (stemmedWord.Length >4)
                        {
                            stemmedWord = stemmedWord.Substring(0, 4);
                        }
                        stemmed.Add(stemmedWord);
                    }
                    pos = -1;
                }
                else
                {
                    if (pos == -1)
                        pos = i;
                }
            }

            if (pos != -1 && i - pos > 0)
            {
                string stemmedWord = StemWord(text.Substring(pos, i - pos));
                if (stemmedWord.Length > 4)
                {
                    stemmedWord = stemmedWord.Substring(0, 4);
                }
                stemmed.Add(stemmedWord);
            }

            return stemmed;
        }

        public List<WordData> GetWordList(string text, int line = -1)
        {
            List<WordData> list = new List<WordData>();
            int pos = -1;
            int i;
            for (i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    if (pos != -1 && i - pos > 0)
                    {
                        list.Add(new WordData(text.Substring(pos, i - pos), line, pos, i - 1));
                    }
                    pos = -1;
                }
                else
                {
                    if (pos == -1)
                        pos = i;
                }
            }
            if (pos != -1 && i - pos > 0)
            {
                list.Add(new WordData(text.Substring(pos, i - pos), line, pos, i - 1));
            }
            return list;
        }

        public void StemList(List<WordData> words)
        {
            for (int i = 0; i < words.Count; i++)
            {
                words[i].stemmed = StemWord(words[i].word);
            }
        }

        public abstract string StemWord(string word);

        public string NGramStemWord(string word)
        {
            string stemmedWord = StemWord(word);
            if (stemmedWord.Length > 4)
            {
                stemmedWord = stemmedWord.Substring(0, 4);
            }
            return stemmedWord;
        }
    }

    public class GenericStemmer : Stemmer
    {
        public override string StemWord(string word)
        {
            word = word.ToLower();

            if (word.Length < 4)
                return word;
            if (word.Length == 4)
                return word.Substring(0, word.Length - 1);
            if (word.Length < 7)
                return word.Substring(0, word.Length - 2);
            return word.Substring(0, word.Length - 3);
        }
    }

    public class FullEngilshStemmer : Stemmer
    {
        public FullEngilshStemmer()
        {
            step1Endings = new Dictionary<string, string>();
            step1Endings.Add("sses", "ss");
            step1Endings.Add("ies", "i");
            step1Endings.Add("ss", "ss");// (nothing to be changed!!!)
            step1Endings.Add("eed", "eed");// ??? (the ending d was removed, e.g., 'agreed', however 'feed', 'need', 'indeed' ... all should remain as is!)
            step1Endings.Add("ated", "ate");
            step1Endings.Add("ating", "ate");
            step1Endings.Add("bled", "bl");// and if there is another wowel -> ble
            step1Endings.Add("bling", "bl");// and if there is another wowel -> ble
            step1Endings.Add("ized", "ize");
            step1Endings.Add("izing", "ize");
            step1Endings.Add("ling", "le");
            step1Endings.Add("led", "le");
            step1Endings.Add("sing", "se");
            step1Endings.Add("sed", "se");
            step1Endings.Add("zed", "ze");
            step1Endings.Add("zing", "ze");
            step1Endings.Add("ing", "");
            step1Endings.Add("ed", "");
            step1Endings.Add("s", "");

            step3Endings = new Dictionary<string, string>();
            step3Endings.Add("ational", "ate");
            step3Endings.Add("tional", "tion");
            step3Endings.Add("enci", "ence");
            step3Endings.Add("anci", "ance");
            step3Endings.Add("izer", "ize");
            step3Endings.Add("bli", "ble");
            step3Endings.Add("alli", "al");
            step3Endings.Add("entli", "ent");
            step3Endings.Add("eli", "e");
            step3Endings.Add("ousli", "ous");
            step3Endings.Add("ization", "ize");
            step3Endings.Add("ation", "ate");
            step3Endings.Add("ator", "ate");
            step3Endings.Add("alism", "al");
            step3Endings.Add("iveness", "ive");
            step3Endings.Add("fulness", "ful");
            step3Endings.Add("ousness", "ous");
            step3Endings.Add("aliti", "al");
            step3Endings.Add("iviti", "ive");
            step3Endings.Add("biliti", "ble");
            step3Endings.Add("logi", "log");


            step4Endings = new Dictionary<string, string>();
            step4Endings.Add("icate", "ic");
            step4Endings.Add("ative", "");
            step4Endings.Add("alize", "al");
            step4Endings.Add("iciti", "ic");
            step4Endings.Add("ical", "ic");
            step4Endings.Add("ful", "");
            step4Endings.Add("ness", "");

            step5Endings = new Dictionary<string, string>();
            step5Endings.Add("al", "");
            step5Endings.Add("ance", "");
            step5Endings.Add("ence", "");
            step5Endings.Add("er", "");
            step5Endings.Add("ic", "");
            step5Endings.Add("able", "");
            step5Endings.Add("ible", "");
            step5Endings.Add("ant", "");
            step5Endings.Add("ement", "");
            step5Endings.Add("ment", "");
            step5Endings.Add("ent", "");
            step5Endings.Add("ion", "");
            step5Endings.Add("ou", "");
            step5Endings.Add("ism", "");
            step5Endings.Add("ate", "");
            step5Endings.Add("iti", "");
            step5Endings.Add("ous", "");
            step5Endings.Add("ive", "");
            step5Endings.Add("ize", "");

        }
        private const int MinStemLength = 3;

        private static char[] vowels = { 'a', 'e', 'i', 'u', 'o' };
        private static char[] ambCons = { 'w', 'x', 'y'};
        private static Dictionary<string, string> step1Endings = null;
        private static Dictionary<string, string> step3Endings = null;
        private static Dictionary<string, string> step4Endings = null;
        private static Dictionary<string, string> step5Endings = null;

        private string StemStep0(string word)
        {
            if (word.EndsWith("\'s")) return word.Substring(0, word.Length - 2);
            if (word.EndsWith("\'")) return word.Substring(0, word.Length - 1);
            return word;
        }

        private string StemStep1(string word)
        {
            if (word.Length >= MinStemLength + 5)
            {
                string end5 = word.Substring(word.Length - 5);
                if (step1Endings.ContainsKey(end5))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 5));
                    if (step1Endings[end5] == "bl" && sb.ToString().IndexOfAny(vowels) >= 0)
                    {
                        sb.Append("ble");
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append(step1Endings[end5]);
                        return sb.ToString();
                    }
                }
            }
            if (word.Length >= MinStemLength + 4)
            {
                string end4 = word.Substring(word.Length - 4);
                if (step1Endings.ContainsKey(end4))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 4));
                    if (step1Endings[end4] == "bl" && sb.ToString().IndexOfAny(vowels) >= 0)
                    {
                        sb.Append("ble");
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append(step1Endings[end4]);
                        return sb.ToString();
                    }
                }
            }
            if (word.Length >= MinStemLength + 3)
            {
                string end3 = word.Substring(word.Length - 3);
                if (step1Endings.ContainsKey(end3))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 3));
                    sb.Append(step1Endings[end3]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 2)
            {
                string end2 = word.Substring(word.Length - 2);
                if (step1Endings.ContainsKey(end2))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 2));
                    sb.Append(step1Endings[end2]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 1)
            {
                string end1 = word.Substring(word.Length - 1);
                if (step1Endings.ContainsKey(end1))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 1));
                    sb.Append(step1Endings[end1]);
                    return sb.ToString();
                }
            }
            return word;
        }

        private string StemStep3(string word)
        {
            if (word.Length >= MinStemLength + 7)
            {
                string end7 = word.Substring(word.Length - 7);
                if (step3Endings.ContainsKey(end7))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 7));
                    sb.Append(step3Endings[end7]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 6)
            {
                string end6 = word.Substring(word.Length - 6);
                if (step3Endings.ContainsKey(end6))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 6));
                    sb.Append(step3Endings[end6]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 5)
            {
                string end5 = word.Substring(word.Length - 5);
                if (step3Endings.ContainsKey(end5))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 5));
                    sb.Append(step3Endings[end5]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 4)
            {
                string end4 = word.Substring(word.Length - 4);
                if (step3Endings.ContainsKey(end4))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 4));
                    sb.Append(step3Endings[end4]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 3)
            {
                string end3 = word.Substring(word.Length - 3);
                if (step3Endings.ContainsKey(end3))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 3));
                    sb.Append(step3Endings[end3]);
                    return sb.ToString();
                }
            }
            return word;
        }


        private string StemStep4(string word)
        {
            if (word.Length >= MinStemLength + 5)
            {
                string end5 = word.Substring(word.Length - 5);
                if (step4Endings.ContainsKey(end5))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 5));
                    sb.Append(step4Endings[end5]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 4)
            {
                string end4 = word.Substring(word.Length - 4);
                if (step4Endings.ContainsKey(end4))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 4));
                    sb.Append(step4Endings[end4]);
                    return sb.ToString();
                }
            }
            if (word.Length >= MinStemLength + 3)
            {
                string end3 = word.Substring(word.Length - 3);
                if (step4Endings.ContainsKey(end3))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 3));
                    sb.Append(step4Endings[end3]);
                    return sb.ToString();
                }
            }
            return word;
        }

        private string StemStep5(string word)
        {
            if (word.Length >= MinStemLength + 5)
            {
                string end5 = word.Substring(word.Length - 5);
                if (step5Endings.ContainsKey(end5))
                {
                    return word.Substring(0, word.Length - 5);
                }
            }
            if (word.Length >= MinStemLength + 4)
            {
                string end4 = word.Substring(word.Length - 4);
                if (step5Endings.ContainsKey(end4))
                {
                    return word.Substring(0, word.Length - 4);
                }
            }
            if (word.Length >= MinStemLength + 3)
            {
                string end3 = word.Substring(word.Length - 3);
                if (step5Endings.ContainsKey(end3))
                {
                    return word.Substring(0, word.Length - 3);
                }
            }
            if (word.Length >= MinStemLength + 2)
            {
                string end2 = word.Substring(word.Length - 2);
                if (step5Endings.ContainsKey(end2))
                {
                    return word.Substring(0, word.Length - 2);
                }
            }
            return word;
        }

        private string StemStep6(string word)
        {
            string res = word;
            if (word.Length >= MinStemLength&& word.EndsWith("e") && cvc(word[word.Length-3],word[word.Length-2],word[word.Length-1]))
            {
                res = res.Substring(0, res.Length - 1);
            }
            if (res.EndsWith("ll")) return res.Substring(0, res.Length - 1);
            return res;
        }

        private bool cvc(char p1, char p2, char p3)
        {
            if (ambCons.Contains(p3)) return false;
            if (!vowels.Contains(p1) && vowels.Contains(p2) && !vowels.Contains(p2)) return true;
            return false;
        }

        private string StemStep2(string word)
        {
            if (word.Length >= MinStemLength + 1)
            {
                char last = word[word.Length - 1];
                if (last == 'y')
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(word.Substring(0, word.Length - 1));
                    if (sb.ToString().IndexOfAny(vowels) >= 0)
                    {
                        sb.Append('i');
                    }
                    return sb.ToString();
                }
            }
            return word;
        }

        public override string StemWord(string word)
        {
            word = word.ToLower();
            string res = StemStep6(StemStep5(StemStep4(StemStep3(StemStep2(StemStep1(StemStep0(word)))))));
            return res;
        }
    }

    public class EngilshStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "s's|ing|'s|s'|ed|s|'".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    public class FullLatvianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> words = "ārā|cik|kad|maz|pus|rīt|sen|šad|šur|tur|žēl|kur|jau|tad|vēl|tik|pie|pēc|gar|par|pār|bez|aiz|zem|dēļ|lai|vai|arī|gan|bet|jeb|būt|esi|būs|kas|kam|kur".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static Dictionary<string, bool> endings = ("amākajiem|ākumiņiem|āmākajiem|ošākajiem|" +
"amākajai|amākajam|amākajām|amākajās|amākajos|ākumiņam|" +
"ākumiņos|ākumiņus|āmākajai|āmākajam|āmākajām|āmākajās|āmākajos|īsieties|" +
"ošākajai|ošākajam|ošākajām|ošākajās|ošākajos|tājiņiem|tākajiem|" +
"amajiem|amākais|amākajā|amākiem|ākajiem|ākumiem|ākumiņa|" +
"ākumiņā|ākumiņi|ākumiņš|ākumiņu|āmajiem|āmākais|āmākajā|āmākiem|ējiņiem|īsimies|" +
"īsities|īšoties|ošajiem|ošākais|ošākajā|ošākiem|sieties|šaniņai|šaniņas|šaniņām|" +
"šaniņās|šaniņos|tājiņai|tājiņam|tājiņas|tājiņām|tājiņās|tājiņos|tājiņus|tākajai|" +
"tākajam|tākajām|tākajās|tākajos|umiņiem|ušajiem|utiņiem|" +
"amajai|amajam|amajām|amajās|amajos|amākai|amākam|amākas|amākām|" +
"amākās|amākie|amākos|amākus|ākajai|ākajam|ākajām|ākajās|ākajos|ākumam|ākumiņ|ākumos|" +
"ākumus|āmajai|āmajam|āmajām|āmajās|āmajos|āmākai|āmākam|āmākas|āmākām|āmākās|āmākie|" +
"āmākos|āmākus|damies|ējiņai|ējiņam|ējiņas|ējiņām|ējiņās|ējiņos|ējiņus|ieties|ošajai|" +
"ošajam|ošajām|ošajās|ošajos|ošākai|ošākam|ošākas|ošākām|ošākās|ošākie|ošākos|ošākus|" +
"simies|sities|sniņai|sniņas|sniņām|sniņās|šaniņa|šaniņā|šaniņu|šoties|tajiem|tājiem|" +
"tājiņa|tājiņā|tājiņi|tājiņš|tājiņu|tākais|tākajā|tākiem|tiņiem|umiņam|umiņos|umiņus|" +
"ušajai|ušajam|ušajām|ušajās|ušajos|utiņam|utiņos|utiņus|" +
"ajiem|amais|amajā|amāka|amākā|amāki|amāko|amāks|amāku|amiem|amies|" +
"aties|ākais|ākajā|ākiem|ākuma|ākumā|ākumi|ākums|ākumu|āmais|āmajā|āmāka|āmākā|āmāki|" +
"āmāko|āmāks|āmāku|āmiem|āmies|āties|damas|damās|ējais|ējiem|ējiņa|ējiņā|ējiņi|ējiņš|" +
"ējiņu|iņiem|īsies|īsiet|īšiem|ošais|ošajā|ošāka|ošākā|ošāki|ošāko|ošāks|ošāku|ošiem|" +
"ošies|oties|sniņa|sniņā|sniņu|šanai|šanas|šanām|šanās|šanos|tajai|tajam|tajām|tajās|" +
"tajos|tājai|tājam|tājas|tājām|tājās|tājiņ|tājos|tājus|tākai|tākam|tākas|tākām|tākās|" +
"tākie|tākos|tākus|tiņai|tiņam|tiņas|tiņām|tiņās|tiņos|tiņus|umiem|umiņa|umiņā|umiņi|" +
"umiņš|umiņu|usies|ušais|ušajā|ušiem|ušies|utiņa|utiņā|utiņi|utiņš|utiņu|" +
"ajai|ajam|ajām|ajās|ajos|amai|amam|amas|amām|amās|amie|amos|amus|" +
"anīs|ākai|ākam|ākas|ākām|ākās|ākie|ākos|ākus|āmai|āmam|āmas|āmām|āmās|āmie|āmos|āmus|" +
"dama|dami|dams|ējai|ējam|ējas|ējām|ējās|ējie|ējiņ|ējos|ējus|inīs|iņai|iņam|iņas|iņām|" +
"iņās|iņos|iņus|īsim|īsit|īšos|īšot|īšus|ītei|ītem|ītes|ītēm|ītēs|ītim|ītis|jiem|ošai|" +
"ošam|ošas|ošām|ošās|ošie|ošos|ošus|sies|siet|sniņ|šana|šanā|šanu|tais|tajā|tāja|tājā|" +
"tāji|tājs|tāju|tāka|tākā|tāki|tāko|tāks|tāku|tiem|ties|tiņa|tiņā|tiņi|tiņš|tiņu|umam|" +
"umiņ|umos|umus|ušai|ušam|ušas|ušām|ušās|ušie|ušos|ušus|utiņ|" +
"ais|ajā|ama|amā|ami|amo|ams|amu|anī|āka|ākā|āki|āko|āks|āku|āma|" +
"āmā|āmi|āmo|āms|āmu|ēja|ējā|ēji|ējo|ējs|ēju|iem|ies|iet|inī|iņa|iņā|iņi|iņš|iņu|īsi|" +
"īša|īši|īšu|īte|ītē|īti|ītī|jām|jās|jos|oša|ošā|oši|ošo|ošs|ošu|sim|sit|šos|šot|tai|" +
"tam|tas|tāj|tām|tās|tie|tiņ|tos|tus|uma|umā|umi|ums|umu|usi|usī|uša|ušā|uši|ušo|ušu|" +
"ai|am|as|at|āk|ām|ās|āt|ei|em|es|ēj|ēm|ēs|ie|ij|im|iņ|is|īm|īs|īt|" +
"ju|mu|os|ot|si|šu|ta|tā|ti|to|ts|tu|um|ur|us|" +
"a|ā|e|ē|i|ī|m|o|s|š|t|u|ū").Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            if (words.ContainsKey(word))
                return word;

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    public class LatvianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> words = "ārā|cik|kad|maz|pus|rīt|sen|šad|šur|tur|žēl|kur|jau|tad|vēl|tik|pie|pēc|gar|par|pār|bez|aiz|zem|dēļ|lai|vai|arī|gan|bet|jeb|būt|esi|būs|kas|kam|kur".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static Dictionary<string, bool> endings = "iem|ais|ies|iet|am|as|ai|ām|ās|os|ie|es|em|ēm|ēs|ij|īm|is|īs|us|um|im|āt|at|it|a|ā|e|ē|i|ī|m|o|s|š|t|u|ū".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            if (words.ContainsKey(word))
                return word;

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    public class GermanStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "stens|erem|eren|erer|eres|stem|sten|ster|stes|test|den|end|ens|ere|ern|est|ien|nen|nes|sen|ses|ste|ten|tet|em|en|er|es|et|im|ns|se|st|ta|te|a|e|n|t|s".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    public class LithuanianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ąįį|ais|ąja|ąją|ąjį|ama|amą|ame|ami|amo|ams|amu|amų|ant|asi|ate|aus|čia|čią|čiu|čių|eis|ėje|ėme|ems|ėms|ėse|ėsi|ęsi|ėte|iai|iam|ias|iąs|iat|iau|iem|ies|įjį|ijų|ima|imą|ime|imi|imo|ims|imu|imų|int|ioj|iom|ion|ios|isi|ite|iui|iuj|ium|iuo|ius|kim|kis|kit|mis|nie|oje|oji|ojo|oma|omą|ome|omi|omo|oms|omu|omų|ose|osi|ote|sai|sią|sim|sis|sit|siu|šią|šim|šis|šit|šiu|tai|tam|tas|tis|tos|tum|tus|tųs|tys|udu|uje|ųjį|ųjų|umi|ums|uos|usi|usį|vęs|vim|vyj|yje|ymą|yme|ymo|ymu|ymų|yse|ai|am|an|as|ąs|at|au|ei|ėj|ėm|ėn|es|ės|ęs|ėt|ia|ią|ie|im|in|io|is|įs|it|iu|ių|ki|ms|oj|om|on|os|ot|si|sų|ši|ta|tą|ti|tį|to|ts|tu|tų|ui|uj|um|uo|us|ūs|ve|vi|vo|yj|ys|a|ą|e|ė|ę|i|į|k|o|s|š|t|u|ų|y".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    public class EstonianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "sse|st|le|lt|ks|ni|na|ta|ga|d|t|s|l".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class GreekStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ου|ης|ας|ος|ες|αν|ον|ών|ων|ιο|ια|να|τα|ι|ς|ό|ν|ο|α|η|ά|ε|ή|ί|ρ|τ".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class DanishStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ingen|en|an|on|in|er|nd|et|ne|te|de|re|le|se|ng|ns|es|is|ts|as|us|rs|\'s|l|m|n|r|d|t|a|v|e|g|k|s|i|y|o|h".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class BulgarianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ата|ите|то|но|во|ни|ки|ци|ли|ти|ри|на|ва|ра|ка|та|ла|ов|ат|ът|не|ин|ен|ан|он|ия|ер|о|д|и|а|м|в|й|с|т|е|к|н|р|л|г".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class CzechStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ova|ové|ová|ovi|ově|ky|ly|ny|ty|vy|ní|ho|no|al|ce|le|ie|se|je|te|ne|ku|ou|mu|tu|nu|ru|la|ra|ta|na|ka|da|ia|ký|ké|né|ti|mi|li|ci|ii|ni|ch|ek|at|it|en|in|on|an|ně|em|um|ům|es|is|as|us|os|ov|ův|er|ng|y|í|o|l|e|u|a|ý|é|á|i|k|t|d|n|m|s|c|ů|r".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }


    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class SwedishStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ingen|ingar|ånare|en|an|on|in|ll|ar|er|at|et|st|na|ra|ta|ka|la|ia|ng|te|de|re|ne|ns|rs|as|ts|es|is|us|os|h|m|n|l|d|r|t|a|g|v|e|s|k|p|i|u|y|o|-".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class SlovenianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ki|ti|li|ni|ci|em|om|ta|la|ja|na|va|ra|ka|ma|sa|ca|ia|je|ne|ke|le|te|ko|no|er|ih|ju|nu|ov|es|is|us|en|an|in|on|i|j|m|a|e|d|t|l|o|r|h|u|v|g|s|k|c|y".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class SlovakStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "mi|ov|om|ie|ne|te|ka|la|ra|ia|ta|na|va|ny|ky|né|ku|ou|nu|ho|ej|ch|es|is|us|er|en|in|on|an|i|m|á|e|c|a|í|y|é|u|o|t|l|ý|ú|d|k|s|r|n|g".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class RussianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ского|нский|вский|ки|ти|ли|ни|ми|ри|ия|ая|да|ла|ра|на|ка|та|га|ма|ва|са|ле|не|те|ре|ке|ку|ну|ру|но|ой|ей|ий|ай|ов|ев|ом|ем|ам|ин|он|ен|ан|ах|ны|ты|ры|ль|ер|ар|и|я|а|е|у|л|к|о|д|м|т|н|х|ы|ь|р|ю|с|г".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class RomanianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "te|re|ne|le|in|an|en|at|ri|ei|ii|ia|ta|na|er|ul|es|e|n|t|i|u|ă|a|d|r|l|c|o|m|s|k|y|g|h".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class PortugueseStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ra|la|ia|da|ta|na|ca|ar|er|am|ri|ni|re|de|te|se|le|ne|is|os|es|us|as|ns|\'s|do|to|io|ou|al|on|an|in|en|ng|a|r|m|i|e|s|o|u|l|d|n|k|c|t|y|h".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class PolishStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "kiego|ne|ie|le|ki|ni|li|ia|na|ta|ra|ka|da|la|sa|ch|em|ny|ng|as|is|es|us|in|en|an|on|ów|er|ę|u|t|z|e|i|a|h|o|d|m|y|k|j|s|n|w|ą|c|l|r".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class DutchStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ingen|en|an|on|in|st|et|at|ar|er|nd|ie|ne|te|re|de|se|le|ae|ts|rs|ns|is|us|es|as|os|\'s|el|al|ch|ng|um|ri|ni|na|ia|ra|ka|ta|la|n|t|r|d|e|s|k|l|h|g|m|o|f|u|i|a|p|y|c|v|-".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class MalteseStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "zjoni|ijiet|ija|ata|ali|ati|iet|hom|la|ja|ra|ta|na|ha|ia|ka|in|an|ni|ri|li|ti|aw|er|es|us|ju|at|a|n|l|i|r|x|d|s|u|t|m|e|h|o|g".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class ItalianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "zione|al|la|ia|ta|da|ra|na|er|ar|te|le|ne|re|se|an|on|in|en|ni|ti|ri|li|si|no|to|io|es|as|is|us|os|ng|l|a|r|e|n|i|o|m|d|s|t|u|c|h".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class HungarianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ban|es|us|nt|st|rt|et|át|it|ok|ak|ek|ik|er|en|on|in|al|ől|ól|re|ai|ni|ja|ra|ia|ta|na|ba|sz|s|y|t|k|m|g|ő|r|n|l|d|e|ó|i|a|z|é|o".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class CroatianStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ne|ke|je|le|te|re|no|ri|li|ti|ki|ni|ci|ja|ma|la|na|ka|da|va|ta|ra|ca|sa|ia|on|an|in|en|er|is|us|es|ju|nu|ku|om|im|ić|ov|e|o|i|a|g|d|k|r|s|u|t|m|j|h|c|l".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class IrishStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "ach|us|is|as|es|ir|ar|er|la|na|ta|ia|aí|in|nn|an|on|en|dh|le|te|ne|il|ng|s|a|í|n|o|h|e|d|i|l|t|m|y".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class FrenchStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "et|es|us|is|as|te|le|re|ne|er|ar|en|on|in|an|ra|ia|na|t|s|e|r|i|c|n|é|m|d|l|u|a|g|k|h|o|y".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class FinnishStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "sesta|oista|sessa|iseen|kseen|ainen|isten|uksen|et|at|it|si|li|ti|ri|ni|na|ka|la|ta|sa|aa|ia|ja|ra|us|es|as|is|os|en|un|an|in|on|yn|tä|iä|le|ne|te|er|ng|t|i|a|s|n|ä|e|o|u|d|y|r|l|m|k|h|-".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    /// <summary>
    /// TODO: The endings are copied from MP_HeuristicStemmer.pm and are statistically motivated and not linguistically (should be revised if possible)!
    /// </summary>
    public class SpanishStemmer : Stemmer
    {
        private readonly static Dictionary<string, bool> endings = "an|on|en|in|al|el|ra|ta|ia|da|ca|na|sa|la|ka|es|os|as|is|us|\'s|er|ar|re|te|de|ne|se|le|ro|do|to|lo|io|no|ni|ri|am|at|ng|n|l|a|s|r|e|o|d|y|z|i|k|m|c|t|h|u|v|-".Split('|').Distinct().ToDictionary(w => w, w => true);
        private readonly static int MaxEndingLength = endings.Keys.Max(e => e.Length);
        private readonly static int MinEndingLength = endings.Keys.Min(e => e.Length);
        private const int MinStemLength = 2;

        public override string StemWord(string word)
        {
            word = word.ToLower();

            for (int i = MaxEndingLength; i >= MinEndingLength; i--)
            {
                if (word.Length >= MinStemLength + i && endings.ContainsKey(word.Substring(word.Length - i)))
                {
                    return word.Substring(0, word.Length - i);
                }
            }

            return word;
        }
    }

    public class WordData
    {
        public WordData()
        {
            word = null;
            stemmed = null;
            line = -1;
            colFrom = -1;
            colTo = -1;
        }
        public WordData(string w, int l, int cf, int ct)
        {
            word = w;
            stemmed = null;
            line = l;
            colFrom = cf;
            colTo = ct;
        }
        public string word;
        public string stemmed;
        public int line;
        public int colFrom;
        public int colTo;
    }
}
