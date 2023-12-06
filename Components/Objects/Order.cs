using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Components.Objects
{
    class Order
    {
        public int OrderId { get; set; }
        public string Item { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
