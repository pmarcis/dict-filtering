using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FilterGizaDictionary
{
    [Serializable]
    [XmlType("KeyValuePairOfStringArrayOfStringProbabEntry")]
    public class KeyValueEntry
    {
        [XmlElement("Key")]
        public string Key;
        [XmlArray("Value")]
        [XmlArrayItem("StringProbabEntry")]
        public List<StringProbabEntry> valueList;

        public KeyValueEntry(string term, List<StringProbabEntry> list)
        {
            Key = term;
            valueList = list;
        }

        public KeyValueEntry()
        {
            Key = null;
            valueList = new List<StringProbabEntry>();
        }
    }

}
