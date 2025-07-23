using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace transaction_manager.Models
{
    public class Operation
    {
        public string Type;
        public int TransactionId;
        public string DataItem;
        public int Moment;
    }
}
