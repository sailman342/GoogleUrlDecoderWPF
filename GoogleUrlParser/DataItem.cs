using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleUrlParser
{
    public class DataItem
    {
        public string StringValue { get; private set; }

        public int ForeNumber { get; private set; }
        public string Letter { get; private set; }
        public string Body { get; private set; }

        // init as not a matrix
        public bool IsMatrixElement { get; private set; } = false;
        public int MatrixSize { get; private set; } = -1;
        public List<DataItem> Childs { get; internal set; } = new List<DataItem> { };

        public DataItem(string itmStrArg)
        {
            StringValue = itmStrArg;

            int index = 0;
            while("0123456789".Contains(itmStrArg.Substring(index,1)))
                {
                index++;
            }
            // General

            ForeNumber = int.Parse( itmStrArg.Substring(0, index));
            Letter = itmStrArg.Substring(index, 1);
            Body = itmStrArg.Substring(index + 1);

            // Matrix element

            if(Letter == "m")
            {
                IsMatrixElement = true;
                MatrixSize = int.Parse(Body);
            }
            
        }
    }
}
