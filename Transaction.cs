using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    internal class Transaction
    {
        string desc;
        string amt;
        string dt;

        public string source_name { get; set; }
        public string description { get; set; }
        public string amount { get; set; }
        public string type { get; set; } = "withdrawal";
        public string date { get; set; }
        public string notes { get; set; } = "Automatically added by bot.";

        public Transaction(string desc, string amt, string dt, string src)
        {
            this.desc = desc;
            this.amt = amt;
            this.dt = dt;
            source_name = src;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(desc)) description = "PENDING DESCRIPTION";
            else description = desc.Trim();

            try
            {
                amount =  double.Parse(amt).ToString("F2", CultureInfo.InvariantCulture);
                // 2022/09/20-13:04:03    originals
                dt = dt.Trim();
                dt = dt.Replace("-", "T");
                dt += "-06:00";
                dt = dt.Replace("/", "-");
                date = dt;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string Info()
        {
            return $"{dt}    {source_name}    ${amount}    {description}";
        }
    }
}
