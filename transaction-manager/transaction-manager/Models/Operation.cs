namespace transaction_manager.Models
{
    public class Operation
    {
        public string Type { get; set; } = ""; 
        public string DataItem { get; set; } = "";
        public string TransactionName { get; set; } = ""; 
        public int Moment { get; set; }
    }
}
