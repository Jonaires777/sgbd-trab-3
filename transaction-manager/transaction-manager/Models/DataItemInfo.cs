using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace transaction_manager.Models
{
    public class DataItemInfo
    {
        public int ReadTS = 0;  
        public int WriteTS = 0; 
    }
}
