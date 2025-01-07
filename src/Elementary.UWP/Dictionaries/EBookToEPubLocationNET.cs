using Elementary.Core.Enums;
using Elementary.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elementary.Dictionaries
{
    public static class EBookToLocation
    {
        public static readonly Dictionary<EBook, int> EBookToEPubLocationNET = new Dictionary<EBook, int>
        {
            { EBook.Genesis, 2 },
            { EBook.Exodus, 53 },
            { EBook.Leviticus, 94 },
            { EBook.Numbers, 122 },
            { EBook.Deuteronomy, 159 },
            { EBook.Joshua, 194 },
            { EBook.Judges, 219 },
            { EBook.Ruth, 241 },
            { EBook.FirstSamuel, 246 },
            { EBook.SecondSamuel, 278 },
            { EBook.FirstKings, 303 },
            { EBook.SecondKings, 326 },
            { EBook.FirstChronicles, 352 },
            { EBook.SecondChronicles, 382 },
            { EBook.Ezra, 419 },
            { EBook.Nehemiah, 430 },
            { EBook.Esther, 444 },
            { EBook.Job, 455 },
            { EBook.Psalms, 498 },
            { EBook.Proverbs, 649 },
            { EBook.Ecclesiastes, 681 },
            { EBook.SongOfSolomon, 694 },
            { EBook.Isaiah, 703 },
            { EBook.Jeremiah, 770 },
            { EBook.Lamentations, 823 },
            { EBook.Ezekiel, 829 },
            { EBook.Daniel, 878 },
            { EBook.Hosea, 891 },
            { EBook.Joel, 906 },
            { EBook.Amos, 910 },
            { EBook.Obadiah, 920 },
            { EBook.Jonah, 922 },
            { EBook.Micah, 927 },
            { EBook.Nahum, 935 },
            { EBook.Habakkuk, 939 },
            { EBook.Zephaniah, 943 },
            { EBook.Haggai, 947 },
            { EBook.Zechariah, 950 },
            { EBook.Malachi, 965 },
            { EBook.Matthew, 970 },
            { EBook.Mark, 999 },
            { EBook.Luke, 1016 },
            { EBook.John, 1041 },
            { EBook.Acts, 1063 },
            { EBook.Romans, 1092 },
            { EBook.FirstCorinthians, 1109 },
            { EBook.SecondCorinthians, 1126 },
            { EBook.Galatians, 1140 },
            { EBook.Ephesians, 1147 },
            { EBook.Philippians, 1154 },
            { EBook.Colossians, 1159 },
            { EBook.FirstThessalonians, 1164 },
            { EBook.SecondThessalonians, 1170 },
            { EBook.FirstTimothy, 1174 },
            { EBook.SecondTimothy, 1181 },
            { EBook.Titus, 1186 },
            { EBook.Philemon, 1190 },
            { EBook.Hebrews, 1192 },
            { EBook.James, 1206 },
            { EBook.FirstPeter, 1212 },
            { EBook.SecondPeter, 1218 },
            { EBook.FirstJohn, 1222 },
            { EBook.SecondJohn, 1228 },
            { EBook.ThirdJohn, 1230 },
            { EBook.Jude, 1232 },
            { EBook.Revelation, 1234 }
        };
    }
}