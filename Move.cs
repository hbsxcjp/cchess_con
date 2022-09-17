using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cchess_con
{
    abstract internal class Move
    {
        public Move(Move before, CoordPair coordPair, string remark)
        {
            Before = before;
            CoordPair =coordPair;
            Remark = remark;
        }


        public Move Before { get; }
        public Move After { get; set; }
        public CoordPair CoordPair { get; }
        public string Remark { get; set; }

        public bool HasOther { get; set; }
    }
}
